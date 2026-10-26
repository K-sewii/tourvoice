using TourVoice.BLL.Dtos;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class PlaceService : IPlaceService
{
    private const string FallbackLanguage = "vi";
    private readonly IPlaceRepository _repository;
    public PlaceService(IPlaceRepository repository) => _repository = repository;

    public async Task<List<PlaceSummaryDto>> GetPublishedDestinationsAsync(string lang)
    {
        var places = await _repository.GetPublishedDestinationsAsync();

        return places.Select(p =>
        {
            var t = ResolveTranslation(p, lang).translation;
            return new PlaceSummaryDto(p.Id, p.Slug, t?.Name ?? p.Slug,
                p.Category, p.CoverImageUrl, p.Latitude, p.Longitude);
        }).ToList();
    }

    public async Task<PlaceDetailDto?> GetPlaceDetailAsync(string slug, string lang)
    {
        var place = await _repository.GetPublishedBySlugAsync(slug);
        if (place is null) return null;

        var (translation, usedLang, fallbackUsed) = ResolveTranslation(place, lang);
        if (translation is null) return null;

        return new PlaceDetailDto(place.Id, place.Slug, translation.Name,
            translation.NarrationText, translation.AudioUrl, translation.AudioDurationSeconds,
            place.Category, place.Latitude, place.Longitude, usedLang, fallbackUsed);
    }

    private static (PlaceTranslation? translation, string languageUsed, bool fallbackUsed)
        ResolveTranslation(Place place, string lang)
    {
        var requested = place.Translations.FirstOrDefault(t => t.LanguageCode == lang);
        if (requested is not null && requested.Status != TranslationStatus.Draft)
            return (requested, lang, false);

        var fallback = place.Translations.FirstOrDefault(t => t.LanguageCode == FallbackLanguage);
        return (fallback, FallbackLanguage, fallback is not null);
    }
}