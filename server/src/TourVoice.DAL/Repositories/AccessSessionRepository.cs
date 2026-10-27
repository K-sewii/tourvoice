using Microsoft.EntityFrameworkCore;
using TourVoice.DAL.Data;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

/// <summary>Phien truy cap (BR-16). Chi ghi khi phien bat dau, mo lai hoac ket thuc - khong ghi moi nhip.</summary>
public interface IAccessSessionRepository
{
    Task<AccessSession?> GetLatestAsync(string deviceKey);
    Task<long> AddAsync(AccessSession session);
    Task ReopenAsync(long sessionId, DateTime lastSeenUtc);
    Task EndAsync(long sessionId, DateTime endedUtc);
}

public class AccessSessionRepository : IAccessSessionRepository
{
    private readonly TourVoiceDbContext _db;
    public AccessSessionRepository(TourVoiceDbContext db) => _db = db;

    public Task<AccessSession?> GetLatestAsync(string deviceKey) =>
        _db.AccessSessions.AsNoTracking()
            .Where(a => a.DeviceKey == deviceKey)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync();

    public async Task<long> AddAsync(AccessSession session)
    {
        _db.AccessSessions.Add(session);
        await _db.SaveChangesAsync();
        return session.Id;
    }

    public Task ReopenAsync(long sessionId, DateTime lastSeenUtc) =>
        _db.AccessSessions.Where(a => a.Id == sessionId).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.EndedAt, (DateTime?)null)
            .SetProperty(a => a.LastSeenAt, lastSeenUtc));

    public Task EndAsync(long sessionId, DateTime endedUtc) =>
        _db.AccessSessions.Where(a => a.Id == sessionId).ExecuteUpdateAsync(s => s
            .SetProperty(a => a.EndedAt, endedUtc)
            .SetProperty(a => a.LastSeenAt, endedUtc));
}
