using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IAdminPlaceService
{
    /// <param name="status">Draft/Published/Hidden hoac null = tat ca.</param>
    Task<PagedResultDto<AdminPlaceNodeDto>> GetTreeAsync(string? status, int page, int pageSize);
    Task<AdminPlaceDetailDto?> GetByIdAsync(int id);
    Task<AdminPlaceDetailDto> CreateAsync(CreatePlaceRequestDto request);
    Task<AdminPlaceDetailDto?> UpdateAsync(int id, UpdatePlaceRequestDto request);

    /// <exception cref="Exceptions.PublishPreconditionsException">BR-08: thieu dieu kien.</exception>
    Task<bool> PublishAsync(int id);
    Task<bool> HideAsync(int id);

    /// <exception cref="Exceptions.DeleteNotAllowedException">BR-13.</exception>
    Task<bool> DeleteAsync(int id);

    /// <summary>Luu anh bia (jpg/png/webp, toi da 5 MB) va gan vao Place. Null neu khong co Place.</summary>
    Task<AdminPlaceDetailDto?> SetCoverAsync(int id, Stream content, long length);
}
