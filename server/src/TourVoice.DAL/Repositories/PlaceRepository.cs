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
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == PlaceStatus.Published);
    }
}