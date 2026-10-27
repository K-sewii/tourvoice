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
                p.Category, p.CoverImageUrl, p.Latitude, p.Longitude,
                p.Kind.ToString(), p.FoodType, p.Address, p.PriceRange, p.OpeningHours);
        }).ToList();
    }

    public async Task<List<FoodPlaceSummaryDto>> GetPublishedFoodsAsync(string lang)
    {
        var foods = await _repository.GetPublishedFoodsAsync();

        return foods.Select(p =>
        {
            var t = ResolveTranslation(p, lang).translation;
            return new FoodPlaceSummaryDto(p.Id, p.Slug, t?.Name ?? p.Slug, p.FoodType,
                p.CoverImageUrl, p.Address, p.PriceRange, p.OpeningHours);
        }).ToList();
    }

    public async Task<PlaceDetailDto?> GetPlaceDetailAsync(string slug, string lang)
    {
        var place = await _repository.GetPublishedBySlugAsync(slug);
        if (place is null) return null;

        var (translation, usedLang, fallbackUsed) = ResolveTranslation(place, lang);
        if (translation is null) return null;

        var foodPlaces = place.Children
            .Where(c => c.Kind == PlaceKind.Food)
            .Select(c =>
            {
                var ct = ResolveTranslation(c, lang).translation;
                return new FoodPlaceSummaryDto(c.Id, c.Slug, ct?.Name ?? c.Slug, c.FoodType,
                    c.CoverImageUrl, c.Address, c.PriceRange, c.OpeningHours);
            }).ToList();

        return new PlaceDetailDto(place.Id, place.Slug, translation.Name,
            translation.NarrationText, translation.AudioUrl, translation.AudioDurationSeconds,
            place.Category, place.Latitude, place.Longitude, usedLang, fallbackUsed,
            place.Kind.ToString(), place.FoodType, place.Address, place.PriceRange, place.OpeningHours,
            foodPlaces);
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