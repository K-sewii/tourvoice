using TourVoice.BLL.Dtos;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    public CustomerService(ICustomerRepository repository) => _repository = repository;

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await _repository.GetUserByIdAsync(userId);
        return user is null ? null :
            new UserProfileDto(user.Id, user.Email, user.FullName, user.Role.ToString(), user.PreferredLanguageCode);
    }

    public async Task<List<FavoritePlaceDto>> GetFavoritesAsync(int userId)
    {
        var favorites = await _repository.GetFavoritesAsync(userId);
        // Luu y: Place chua duoc Include trong repo nay; Buoc sau se bo sung join neu can hien Name/Slug day du.
        return favorites.Select(f => new FavoritePlaceDto(f.PlaceId, "", "", f.CreatedAt)).ToList();
    }

    public Task AddFavoriteAsync(int userId, int placeId) => _repository.AddFavoriteAsync(userId, placeId);
    public Task RemoveFavoriteAsync(int userId, int placeId) => _repository.RemoveFavoriteAsync(userId, placeId);

    public async Task<List<VisitedPlaceDto>> GetVisitsAsync(int userId)
    {
        var visits = await _repository.GetVisitsAsync(userId);
        return visits.Select(v => new VisitedPlaceDto(v.PlaceId, "", "", v.VisitedAt)).ToList();
    }

    public Task MarkVisitedAsync(int userId, int placeId) => _repository.MarkVisitedAsync(userId, placeId);

    public async Task<List<ListenHistoryDto>> GetHistoryAsync(int userId)
    {
        var history = await _repository.GetHistoryAsync(userId);
        return history.Select(h => new ListenHistoryDto(h.PlaceId, h.LanguageCode, h.ListenedSeconds, h.PlayedAt)).ToList();
    }
}