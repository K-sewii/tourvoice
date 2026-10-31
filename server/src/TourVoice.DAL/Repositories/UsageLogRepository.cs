using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class UsageLogRepository : IUsageLogRepository
{
    private readonly TourVoiceDbContext _db;
    public UsageLogRepository(TourVoiceDbContext db) => _db = db;

    public async Task<int> GetCharacterTotalSinceAsync(UsageService service, DateTime fromUtc)
    {
        return await _db.UsageLogs
            .Where(u => u.Service == service && u.CreatedAt >= fromUtc)
            .SumAsync(u => (int?)u.Characters) ?? 0;
    }

    public void Add(UsageLog log) => _db.UsageLogs.Add(log);

    public Task<List<UsageLog>> GetSinceAsync(UsageService service, DateTime fromUtc) =>
        _db.UsageLogs.AsNoTracking()
            .Where(u => u.Service == service && u.CreatedAt >= fromUtc)
            .ToListAsync();

    public Task<List<UsageLog>> GetRecentAsync(UsageService service, int count) =>
        _db.UsageLogs.AsNoTracking()
            .Where(u => u.Service == service)
            .OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
            .Take(count)
            .ToListAsync();
}
