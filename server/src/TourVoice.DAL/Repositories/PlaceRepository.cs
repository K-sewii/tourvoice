using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class PlaceRepository : IPlaceRepository
{
    private readonly TourVoiceDbContext _db;
    public PlaceRepository(TourVoiceDbContext db) => _db = db;

    public async Task<List<Place>> GetPublishedDestinationsAsync()
    {
        return await _db.Places.AsNoTracking()
            .Include(p => p.Translations)
            .Where(p => p.Status == PlaceStatus.Published
                && p.Kind == PlaceKind.Destination
                && p.ParentId == null)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

      public async Task<Place?> GetPublishedBySlugAsync(string slug)
    {
        return await _db.Places.AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Children.Where(c => c.Status == PlaceStatus.Published))
                .ThenInclude(c => c.Translations)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PlaceStatus.Published);
    }

    public Task<Place?> GetByIdAsync(int id) =>
        _db.Places.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<(List<Place> Items, int Total)> GetAdminListAsync(int page, int pageSize)
    {
        var query = _db.Places.AsNoTracking().Include(p => p.Translations).OrderBy(p => p.SortOrder);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public Task<bool> SlugExistsAsync(string slug) => _db.Places.AnyAsync(p => p.Slug == slug);

    public async Task AddAsync(Place place)
    {
        _db.Places.Add(place);
        await _db.SaveChangesAsync();
    }
}