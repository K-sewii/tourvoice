using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IPlaceService
{
    Task<List<PlaceSummaryDto>> GetPublishedDestinationsAsync(string lang);
    Task<PlaceDetailDto?> GetPlaceDetailAsync(string slug, string lang);
}