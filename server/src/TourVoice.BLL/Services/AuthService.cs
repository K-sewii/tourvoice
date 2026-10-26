using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TourVoice.BLL.Auth;
using TourVoice.BLL.Dtos;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class AuthService : IAuthService
{
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
        if (await _repository.EmailExistsAsync(request.Email))
            throw new EmailAlreadyExistsException();

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = UserRole.Customer
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        await _repository.AddUserAsync(user);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
    {
        var user = await _repository.GetUserByEmailAsync(request.Email);
        if (user is null || !user.IsActive) return null;

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed) return null;

        return BuildAuthResponse(user);
    }

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

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDto(tokenString, user.Email, user.FullName, user.Role.ToString());
    }
}