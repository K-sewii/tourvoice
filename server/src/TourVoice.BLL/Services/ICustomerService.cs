using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface ICustomerService
{
    /// <exception cref="Exceptions.UnauthorizedException">Token hop le nhung tai khoan khong con.</exception>
    Task<UserProfileDto> GetProfileAsync(int userId);

    Task<List<FavoritePlaceDto>> GetFavoritesAsync(int userId, string lang);

    /// <exception cref="Exceptions.NotFoundException">Place khong ton tai hoac chua Published.</exception>
    Task AddFavoriteAsync(int userId, int placeId);
    Task RemoveFavoriteAsync(int userId, int placeId);

    Task<VisitsDto> GetVisitsAsync(int userId, string lang);

    /// <exception cref="Exceptions.NotFoundException">Place khong ton tai hoac chua Published.</exception>
    Task MarkVisitedAsync(int userId, int placeId);

    Task<List<ListenHistoryDto>> GetHistoryAsync(int userId, string lang);
}
