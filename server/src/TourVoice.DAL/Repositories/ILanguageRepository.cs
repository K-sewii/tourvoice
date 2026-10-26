using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Repositories;

public interface ILanguageRepository
{
    Task<List<Language>> GetEnabledAsync();
}