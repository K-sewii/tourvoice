using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface IUsageLogRepository
{
    Task<int> GetTodayCharacterTotalAsync(UsageService service);
    Task AddAsync(UsageLog log);
}