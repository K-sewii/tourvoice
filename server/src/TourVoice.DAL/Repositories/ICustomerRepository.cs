using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface ICustomerRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task AddUserAsync(User user);

    Task<List<Favorite>> GetFavoritesAsync(int userId);
    Task<bool> IsFavoriteAsync(int userId, int placeId);
    Task AddFavoriteAsync(int userId, int placeId);
    Task RemoveFavoriteAsync(int userId, int placeId);

    Task<List<Visit>> GetVisitsAsync(int userId);
    Task MarkVisitedAsync(int userId, int placeId);

    Task<List<ListenEvent>> GetHistoryAsync(int userId);
}