using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IAdminPlaceService
{
    Task<PagedResultDto<AdminPlaceListItemDto>> GetListAsync(int page, int pageSize);
    Task<AdminPlaceDetailDto?> GetByIdAsync(int id);
    Task<AdminPlaceDetailDto> CreateAsync(CreatePlaceRequestDto request);
    Task<AdminPlaceDetailDto?> UpdateAsync(int id, UpdatePlaceRequestDto request);
    Task<bool> PublishAsync(int id);
    Task<bool> HideAsync(int id);
}

public class SlugAlreadyExistsException : Exception
{
    public SlugAlreadyExistsException() : base("Slug da ton tai.") { }
}