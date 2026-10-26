using TourVoice.BLL.Dtos;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class LanguageService : ILanguageService
{
    private readonly ILanguageRepository _repository;
    public LanguageService(ILanguageRepository repository) => _repository = repository;

    public async Task<List<LanguageDto>> GetLanguagesAsync()
    {
        var languages = await _repository.GetEnabledAsync();
        return languages.Select(l => new LanguageDto(l.Code, l.VoiceName, l.IsEnabled)).ToList();
    }
}