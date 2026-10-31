using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface IUsageLogRepository
{
    /// <summary>
    /// Tong ky tu da gui tu <paramref name="fromUtc"/> (ke ca lan loi - MyMemory van tinh), nguon cua BR-06.
    /// BLL quyet dinh moc ngay (gio Viet Nam) va truyen xuong; DAL khong tu goi dong ho.
    /// </summary>
    Task<int> GetCharacterTotalSinceAsync(UsageService service, DateTime fromUtc);

    /// <summary>
    /// Chi them vao DbContext, khong luu: BLL goi SaveChanges mot lan de ban dich va dong nhat ky
    /// nam trong cung mot transaction.
    /// </summary>
    void Add(UsageLog log);

    Task<List<UsageLog>> GetSinceAsync(UsageService service, DateTime fromUtc);
    Task<List<UsageLog>> GetRecentAsync(UsageService service, int count);
}
