using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class PlaceRepository : IPlaceRepository
{
    private readonly TourVoiceDbContext _db;
    public PlaceRepository(TourVoiceDbContext db) => _db = db;

    /// <summary>Khach chi thay Place Published; mon con can them cha Published (an diem den thi an ca mon).</summary>
    private static readonly Expression<Func<Place, bool>> VisibleToGuests = p =>
        p.Status == PlaceStatus.Published && (p.ParentId == null || p.Parent!.Status == PlaceStatus.Published);

    public Task<List<Place>> GetPublishedDestinationsAsync() =>
        _db.Places.AsNoTracking()
            .Include(p => p.Translations)
            .Where(p => p.Status == PlaceStatus.Published && p.Kind == PlaceKind.Destination)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

    public Task<List<Place>> GetPublishedCatalogAsync() =>
        _db.Places.AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Parent).ThenInclude(parent => parent!.Translations)
            .Where(VisibleToGuests)
            .OrderBy(p => p.ParentId == null ? p.SortOrder : p.Parent!.SortOrder)
            .ThenBy(p => p.ParentId != null)
            .ThenBy(p => p.SortOrder)
            .AsSplitQuery()
            .ToListAsync();

    public Task<Place?> GetPublishedBySlugAsync(string slug) =>
        _db.Places.AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Parent).ThenInclude(parent => parent!.Translations)
            .Include(p => p.Children.Where(c => c.Status == PlaceStatus.Published).OrderBy(c => c.SortOrder))
                .ThenInclude(c => c.Translations)
            .Where(VisibleToGuests)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Slug == slug);

    public Task<int?> GetPublishedIdBySlugAsync(string slug) =>
        _db.Places.AsNoTracking()
            .Where(VisibleToGuests)
            .Where(p => p.Slug == slug)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

    public Task<bool> IsPublishedAsync(int placeId) =>
        _db.Places.Where(VisibleToGuests).AnyAsync(p => p.Id == placeId);

    public Task<int> CountPublishedDestinationsAsync() =>
        _db.Places.CountAsync(p => p.Status == PlaceStatus.Published && p.Kind == PlaceKind.Destination);

    public Task<Place?> GetByIdAsync(int id) =>
        _db.Places.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == id);

    public Task SaveChangesAsync() => _db.SaveChangesAsync();

    public async Task<(List<Place> Items, int Total)> GetAdminTreeAsync(PlaceStatus? status, int page, int pageSize)
    {
        var query = _db.Places.AsNoTracking().Where(p => p.ParentId == null);
        if (status is { } s)
            query = query.Where(p => p.Status == s || p.Children.Any(c => c.Status == s));

        var total = await query.CountAsync();
        var items = await query
            .Include(p => p.Translations)
            .Include(p => p.Children.OrderBy(c => c.SortOrder)).ThenInclude(c => c.Translations)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();
        return (items, total);
    }

    public Task<Place?> GetAdminDetailAsync(int id) =>
        _db.Places.AsNoTracking()
            .Include(p => p.Translations)
            .Include(p => p.Parent).ThenInclude(parent => parent!.Translations)
            .Include(p => p.Children.OrderBy(c => c.SortOrder)).ThenInclude(c => c.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Place>> GetAllWithTranslationsAsync()
    {
        var all = await _db.Places.AsNoTracking().Include(p => p.Translations).AsSplitQuery().ToListAsync();
        // Diem den theo SortOrder, moi diem den keo theo cac mon con theo SortOrder.
        var parents = all.Where(p => p.ParentId == null).OrderBy(p => p.SortOrder).ThenBy(p => p.Id);
        return parents.SelectMany(parent => new[] { parent }
            .Concat(all.Where(c => c.ParentId == parent.Id).OrderBy(c => c.SortOrder).ThenBy(c => c.Id))).ToList();
    }

    public Task<bool> SlugExistsAsync(string slug) => _db.Places.AnyAsync(p => p.Slug == slug);

    public Task<bool> HasChildrenAsync(int id) => _db.Places.AnyAsync(p => p.ParentId == id);

    public async Task AddAsync(Place place)
    {
        _db.Places.Add(place);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Place place)
    {
        _db.Places.Remove(place);
        await _db.SaveChangesAsync();
    }
}
