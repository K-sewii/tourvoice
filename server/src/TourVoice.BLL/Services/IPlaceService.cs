using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface IPlaceService
{
    Task<List<PlaceSummaryDto>> GetPublishedDestinationsAsync(string lang);

    /// <param name="customerId">Id khach khi request kem JWT Customer - de tra isFavorite/isVisited.</param>
    /// <exception cref="Exceptions.NotFoundException">Slug khong ton tai hoac chua Published.</exception>
    Task<PlaceDetailDto> GetPlaceDetailAsync(string slug, string lang, int? customerId = null);

    /// <summary>Mon cua mot diem den (GET /places/{slug}/foods).</summary>
    /// <exception cref="Exceptions.NotFoundException">Khong phai diem den Published.</exception>
    Task<List<FoodPlaceSummaryDto>> GetFoodsOfDestinationAsync(string slug, string lang);

    /// <summary>Trang Kham pha: category la ma danh muc, "food" (moi mon) hoac rong; q tim khong dau.</summary>
    Task<List<ExploreItemDto>> ExploreAsync(string lang, string? category, string? q);

    Task<List<MapMarkerDto>> GetMapMarkersAsync(string lang);
}
