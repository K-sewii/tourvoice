using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TourVoice.BLL.Auth;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class AuthService : IAuthService
{
    public const int MinPasswordLength = 10;   // BR-12: tang tu 8 ky tu toi 10 ky tu de bao mat tot hon
    public const int MaxFullNameLength = 100;

    private readonly ICustomerRepository _repository;
    private readonly JwtSettings _jwt;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(ICustomerRepository repository, IOptions<JwtSettings> jwtOptions)
    {
        _repository = repository;
        _jwt = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        if (!IsValidEmail(email))
            throw new ValidationException("invalid_email", "Email không hợp lệ.");
        if ((request.Password ?? "").Length < MinPasswordLength)
            throw new ValidationException("weak_password", $"Mật khẩu cần ít nhất {MinPasswordLength} ký tự.");
        var fullName = request.FullName?.Trim() ?? "";
        if (fullName.Length is 0 or > MaxFullNameLength)
            throw new ValidationException("invalid_full_name", "Vui lòng nhập họ tên (tối đa 100 ký tự).");

        if (await _repository.EmailExistsAsync(email))
            throw new EmailAlreadyExistsException();

        var user = new User { Email = email, FullName = fullName, Role = UserRole.Customer };
        user.PasswordHash = _hasher.HashPassword(user, request.Password!);
        await _repository.AddUserAsync(user);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _repository.GetUserByEmailAsync(email);
        if (user is null)
            throw new UnauthorizedException("invalid_credentials", "Email hoặc mật khẩu không đúng.");

        var pwVerified = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? "") == PasswordVerificationResult.Success;
        if (!pwVerified)
            throw new UnauthorizedException("invalid_credentials", "Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw new ForbiddenException("account_locked", "Tài khoản đã bị khóa.");

        return BuildAuthResponse(user);
    }

    internal static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    internal static bool IsValidEmail(string email) =>
        email.Length <= 254 && MailAddress.TryCreate(email, out var parsed) && parsed.Address == email;

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var lifetime = user.Role == UserRole.Admin
            ? TimeSpan.FromHours(_jwt.AdminExpiryHours)
            : TimeSpan.FromDays(_jwt.CustomerExpiryDays);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponseDto(tokenString, user.Email, user.FullName, user.Role.ToString());
    }
}
