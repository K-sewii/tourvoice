using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IListenService
{
    Task<bool> RecordListenAsync(string slug, RecordListenRequestDto request, int? userId);
}