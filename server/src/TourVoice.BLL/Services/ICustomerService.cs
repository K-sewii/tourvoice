using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface ICustomerService
{
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<List<FavoritePlaceDto>> GetFavoritesAsync(int userId);
    Task AddFavoriteAsync(int userId, int placeId);
    Task RemoveFavoriteAsync(int userId, int placeId);
    Task<List<VisitedPlaceDto>> GetVisitsAsync(int userId);
    Task MarkVisitedAsync(int userId, int placeId);
    Task<List<ListenHistoryDto>> GetHistoryAsync(int userId);
}