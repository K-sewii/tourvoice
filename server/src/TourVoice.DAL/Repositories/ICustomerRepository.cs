using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface ICustomerRepository
{
    /// <summary>Email da chuan hoa (trim + chu thuong) tu BLL.</summary>
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(int id);
    Task<bool> EmailExistsAsync(string email);
    Task AddUserAsync(User user);

    /// <summary>Kem Place (+ ban dich, cha) de BLL tra slug/ten dung ngon ngu.</summary>
    Task<List<Favorite>> GetFavoritesAsync(int userId);
    Task<bool> IsFavoriteAsync(int userId, int placeId);
    Task AddFavoriteAsync(int userId, int placeId);
    Task RemoveFavoriteAsync(int userId, int placeId);

    Task<List<Visit>> GetVisitsAsync(int userId);
    Task<bool> IsVisitedAsync(int userId, int placeId);
    Task MarkVisitedAsync(int userId, int placeId);

    /// <summary>50 luot nghe moi nhat, kem Place (+ ban dich, cha).</summary>
    Task<List<ListenEvent>> GetHistoryAsync(int userId);
}
