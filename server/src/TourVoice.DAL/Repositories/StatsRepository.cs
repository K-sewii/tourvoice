using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

/// <summary>So lieu cho dashboard admin (SCR-A2). Chi doc; BLL gom nhom theo ngay (mui gio Asia/Ho_Chi_Minh).</summary>
public interface IStatsRepository
{
    Task<List<DateTime>> GetListenTimesSinceAsync(DateTime fromUtc);

    /// <summary>Phien bat dau, dang mo, hoac ket thuc sau <paramref name="fromUtc"/>.</summary>
    Task<List<AccessSession>> GetSessionsSinceAsync(DateTime fromUtc);

    Task<int> CountPublishedPlacesAsync();

    /// <summary>Ban dich co noi dung nhung con Draft - viec dang cho admin duyet.</summary>
    Task<int> CountDraftTranslationsAsync();
}

public class StatsRepository : IStatsRepository
{
    private readonly TourVoiceDbContext _db;
    public StatsRepository(TourVoiceDbContext db) => _db = db;

    public Task<List<DateTime>> GetListenTimesSinceAsync(DateTime fromUtc) =>
        _db.ListenEvents.AsNoTracking().Where(l => l.PlayedAt >= fromUtc).Select(l => l.PlayedAt).ToListAsync();

    public Task<List<AccessSession>> GetSessionsSinceAsync(DateTime fromUtc) =>
        _db.AccessSessions.AsNoTracking()
            .Where(a => a.StartedAt >= fromUtc || a.EndedAt == null || a.EndedAt >= fromUtc)
            .ToListAsync();

    public Task<int> CountPublishedPlacesAsync() => _db.Places.CountAsync(p => p.Status == PlaceStatus.Published);

    public Task<int> CountDraftTranslationsAsync() =>
        _db.PlaceTranslations.CountAsync(t =>
            t.Status == TranslationStatus.Draft && t.NarrationText != null && t.NarrationText != "");
}
