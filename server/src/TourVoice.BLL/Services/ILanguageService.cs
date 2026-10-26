using TourVoice.BLL.Dtos;

namespace TourVoice.BLL.Services;

public interface ILanguageService
{
    Task<List<LanguageDto>> GetLanguagesAsync();
}