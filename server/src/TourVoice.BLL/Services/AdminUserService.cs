using Microsoft.AspNetCore.Identity;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IUserRepository _repository;
    private readonly PasswordHasher<User> _hasher = new();

    public AdminUserService(IUserRepository repository) => _repository = repository;

    public async Task<PagedResultDto<AdminUserDto>> GetListAsync(string? search, string? role, int page, int pageSize)
    {
        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
                throw new ValidationException("invalid_role", "Vai trò phải là Customer hoặc Admin.");
            roleFilter = parsed;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();
        var (items, total) = await _repository.GetPageAsync(term, roleFilter, page, pageSize);
        return new PagedResultDto<AdminUserDto>(items.Select(ToDto).ToList(), total, page, pageSize);
    }

    public async Task<AdminUserDto> CreateAdminAsync(CreateAdminRequestDto request)
    {
        var email = AuthService.NormalizeEmail(request.Email);
        if (!AuthService.IsValidEmail(email))
            throw new ValidationException("invalid_email", "Email không hợp lệ.");
        if ((request.Password ?? "").Length < AuthService.MinPasswordLength)
            throw new ValidationException("weak_password", $"Mật khẩu cần ít nhất {AuthService.MinPasswordLength} ký tự.");
        var fullName = request.FullName?.Trim() ?? "";
        if (fullName.Length is 0 or > AuthService.MaxFullNameLength)
            throw new ValidationException("invalid_full_name", "Vui lòng nhập họ tên (tối đa 100 ký tự).");
        if (await _repository.EmailExistsAsync(email))
            throw new EmailAlreadyExistsException();

        var user = new User { Email = email, FullName = fullName, Role = UserRole.Admin };
        user.PasswordHash = _hasher.HashPassword(user, request.Password!);
        await _repository.AddAsync(user);
        return ToDto(user);
    }

    public async Task<AdminUserDto?> SetActiveAsync(int userId, bool isActive, int actingUserId)
    {
        var user = await _repository.GetByIdAsync(userId);
        if (user is null) return null;
        if (user.IsActive == isActive) return ToDto(user);

        if (!isActive)
        {
            if (userId == actingUserId)
                throw new BusinessRuleException("cannot_lock_self", "Bạn không thể tự khóa tài khoản của mình.");
            // BR-11: luon con it nhat mot Admin dang hoat dong.
            if (user.Role == UserRole.Admin && await _repository.CountActiveAdminsAsync() <= 1)
                throw new LastAdminException();
        }

        user.IsActive = isActive;
        await _repository.SaveChangesAsync();
        return ToDto(user);
    }

    private static AdminUserDto ToDto(User u) => new(u.Id, u.Email, u.FullName, u.Role.ToString(), u.IsActive);
}
