using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface IUsageLogRepository
{
    /// <summary>Tong ky tu da gui trong ngay UTC (ke ca lan loi - MyMemory van tinh), nguon cua BR-06.</summary>
    Task<int> GetTodayCharacterTotalAsync(UsageService service);

    /// <summary>
    /// Chi them vao DbContext, khong luu: BLL goi SaveChanges mot lan de ban dich va dong nhat ky
    /// nam trong cung mot transaction.
    /// </summary>
    void Add(UsageLog log);

    Task<List<UsageLog>> GetSinceAsync(UsageService service, DateTime fromUtc);
    Task<List<UsageLog>> GetRecentAsync(UsageService service, int count);
}
