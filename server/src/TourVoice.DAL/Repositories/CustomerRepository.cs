using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private const int HistoryLimit = 50;

    private readonly TourVoiceDbContext _db;
    public CustomerRepository(TourVoiceDbContext db) => _db = db;

    public Task<User?> GetUserByEmailAsync(string email) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetUserByIdAsync(int id) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

    public Task<bool> EmailExistsAsync(string email) =>
        _db.Users.AsNoTracking().AnyAsync(u => u.Email == email);

    public async Task AddUserAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public Task<List<Favorite>> GetFavoritesAsync(int userId) =>
        _db.Favorites.AsNoTracking()
            .Include(f => f.Place).ThenInclude(p => p.Translations)
            .Include(f => f.Place).ThenInclude(p => p.Parent)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .AsSplitQuery()
            .ToListAsync();

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
        _db.Visits.AsNoTracking()
            .Include(v => v.Place).ThenInclude(p => p.Translations)
            .Include(v => v.Place).ThenInclude(p => p.Parent)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.VisitedAt)
            .AsSplitQuery()
            .ToListAsync();

    public Task<bool> IsVisitedAsync(int userId, int placeId) =>
        _db.Visits.AnyAsync(v => v.UserId == userId && v.PlaceId == placeId);

    public async Task MarkVisitedAsync(int userId, int placeId)
    {
        if (await IsVisitedAsync(userId, placeId)) return;
        _db.Visits.Add(new Visit { UserId = userId, PlaceId = placeId });
        await _db.SaveChangesAsync();
    }

    public Task<List<ListenEvent>> GetHistoryAsync(int userId) =>
        _db.ListenEvents.AsNoTracking()
            .Include(l => l.Place).ThenInclude(p => p.Translations)
            .Include(l => l.Place).ThenInclude(p => p.Parent)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.PlayedAt)
            .Take(HistoryLimit)
            .AsSplitQuery()
            .ToListAsync();
}
