using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IAdminUserService
{
    Task<PagedResultDto<AdminUserDto>> GetListAsync(string? search, string? role, int page, int pageSize);

    /// <summary>Tao tai khoan Admin (chi Admin khac moi tao duoc, PRD ch.07).</summary>
    Task<AdminUserDto> CreateAdminAsync(CreateAdminRequestDto request);

    /// <summary>Khoa/mo khoa. BR-11: khong khoa Admin cuoi cung va khong tu khoa minh.</summary>
    /// <returns>Null neu khong co tai khoan.</returns>
    Task<AdminUserDto?> SetActiveAsync(int userId, bool isActive, int actingUserId);
}
