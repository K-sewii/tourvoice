using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface ILanguageRepository
{
    Task<List<Language>> GetEnabledAsync();
    Task<List<Language>> GetAllAsync();

    // Khong AsNoTracking - admin sua roi SaveChangesAsync
    Task<Language?> GetByCodeAsync(string code);
    Task SaveChangesAsync();
}
