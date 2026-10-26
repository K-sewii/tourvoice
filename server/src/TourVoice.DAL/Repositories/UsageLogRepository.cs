using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public class UsageLogRepository : IUsageLogRepository
{
    private readonly TourVoiceDbContext _db;
    public UsageLogRepository(TourVoiceDbContext db) => _db = db;

    public async Task<int> GetTodayCharacterTotalAsync(UsageService service)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await _db.UsageLogs
            .Where(u => u.Service == service && u.Success && u.CreatedAt >= todayUtc)
            .SumAsync(u => (int?)u.Characters) ?? 0;
    }

    public async Task AddAsync(UsageLog log)
    {
        _db.UsageLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}