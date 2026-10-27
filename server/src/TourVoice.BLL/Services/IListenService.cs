using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IListenService
{
    /// <summary>Ghi mot luot nghe (BR-09). userId null voi khach vang lai.</summary>
    /// <exception cref="Exceptions.ValidationException">Thoi luong ngoai [10, 3600] giay hoac ngon ngu khong bat.</exception>
    /// <exception cref="Exceptions.NotFoundException">Place khong ton tai hoac chua Published.</exception>
    Task RecordAsync(ListenEventRequestDto request, int? userId);
}
