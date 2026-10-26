using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly TourVoiceDbContext _db;
    public CustomerRepository(TourVoiceDbContext db) => _db = db;

    public Task<User?> GetUserByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetUserByIdAsync(int id) =>
        _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<bool> EmailExistsAsync(string email) =>
        _db.Users.AnyAsync(u => u.Email == email);

    public async Task AddUserAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public Task<List<Favorite>> GetFavoritesAsync(int userId) =>
        _db.Favorites.AsNoTracking().Where(f => f.UserId == userId).ToListAsync();

    public Task<bool> IsFavoriteAsync(int userId, int placeId) =>
        _db.Favorites.AnyAsync(f => f.UserId == userId && f.PlaceId == placeId);

    public async Task AddFavoriteAsync(int userId, int placeId)
    {
        if (await IsFavoriteAsync(userId, placeId)) return;
        _db.Favorites.Add(new Favorite { UserId = userId, PlaceId = placeId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveFavoriteAsync(int userId, int placeId)
    {
        var fav = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PlaceId == placeId);
        if (fav is null) return;
        _db.Favorites.Remove(fav);
        await _db.SaveChangesAsync();
    }

    public Task<List<Visit>> GetVisitsAsync(int userId) =>
        _db.Visits.AsNoTracking().Where(v => v.UserId == userId).ToListAsync();

    public async Task MarkVisitedAsync(int userId, int placeId)
    {
        var exists = await _db.Visits.AnyAsync(v => v.UserId == userId && v.PlaceId == placeId);
        if (exists) return;
        _db.Visits.Add(new Visit { UserId = userId, PlaceId = placeId });
        await _db.SaveChangesAsync();
    }

    public Task<List<ListenEvent>> GetHistoryAsync(int userId) =>
        _db.ListenEvents.AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.PlayedAt)
            .Take(50)
            .ToListAsync();
}