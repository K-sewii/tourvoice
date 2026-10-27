using System.Globalization;
using System.Text;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Localization;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class PlaceService : IPlaceService
{
    public const string FoodFilter = "food";

    private readonly IPlaceRepository _places;
    private readonly ICustomerRepository _customers;

    public PlaceService(IPlaceRepository places, ICustomerRepository customers)
    {
        _places = places;
        _customers = customers;
    }

    public async Task<List<PlaceSummaryDto>> GetPublishedDestinationsAsync(string lang)
    {
        var places = await _places.GetPublishedDestinationsAsync();
        return places.Select(p => new PlaceSummaryDto(
            p.Id, p.Slug, Localizer.Name(p, lang), p.Kind.ToString(), p.Category, p.FoodType,
            p.CoverImageUrl, p.Latitude, p.Longitude, p.Address, p.PriceRange, p.OpeningHours)).ToList();
    }

    public async Task<PlaceDetailDto> GetPlaceDetailAsync(string slug, string lang, int? customerId = null)
    {
        var place = await _places.GetPublishedBySlugAsync(slug) ?? throw new PlaceNotFoundException();

        // Place Published nhung chua co ban vi duoc duyet thi khach khong co gi de xem.
        var (translation, usedLang, fallback) = Localizer.Resolve(place, lang);
        if (translation is null) throw new PlaceNotFoundException();

        bool? isFavorite = null, isVisited = null;
        if (customerId is int userId)
        {
            isFavorite = await _customers.IsFavoriteAsync(userId, place.Id);
            isVisited = await _customers.IsVisitedAsync(userId, place.Id);
        }

        return new PlaceDetailDto(
            place.Id, place.Slug, translation.Name, translation.NarrationText,
            place.Kind.ToString(), place.Category, place.FoodType, place.CoverImageUrl,
            place.Latitude, place.Longitude, place.Address, place.PriceRange, place.OpeningHours,
            usedLang, fallback,
            place.Parent?.Slug, place.Parent is null ? null : Localizer.Name(place.Parent, lang),
            place.Children.Where(c => c.Kind == PlaceKind.Food).Select(c => ToFood(c, lang)).ToList(),
            isFavorite, isVisited);
    }

    public async Task<List<FoodPlaceSummaryDto>> GetFoodsOfDestinationAsync(string slug, string lang)
    {
        var place = await _places.GetPublishedBySlugAsync(slug);
        if (place is null || place.Kind != PlaceKind.Destination) throw new PlaceNotFoundException();
        return place.Children.Where(c => c.Kind == PlaceKind.Food).Select(c => ToFood(c, lang)).ToList();
    }

    public async Task<List<ExploreItemDto>> ExploreAsync(string lang, string? category, string? q)
    {
        var places = await _places.GetPublishedCatalogAsync();
        var needle = Fold(q);

        return places
            .Where(p => MatchesCategory(p, category))
            .Select(p => (Place: p, Name: Localizer.Name(p, lang), ParentName: p.Parent is null ? null : Localizer.Name(p.Parent, lang)))
            .Where(x => needle.Length == 0
                || Fold($"{x.Name} {x.Place.Slug} {x.ParentName}").Contains(needle, StringComparison.Ordinal))
            .Select(x => new ExploreItemDto(
                x.Place.Id, x.Place.Slug, x.Name, x.Place.Kind.ToString(), x.Place.Category, x.Place.FoodType,
                x.Place.CoverImageUrl, x.Place.Latitude, x.Place.Longitude,
                x.Place.Address, x.Place.PriceRange, x.Place.OpeningHours,
                x.Place.Parent?.Slug, x.ParentName))
            .ToList();
    }

    public async Task<List<MapMarkerDto>> GetMapMarkersAsync(string lang)
    {
        var places = await _places.GetPublishedCatalogAsync();
        return places.Select(p => new MapMarkerDto(
            p.Id, p.Slug, Localizer.Name(p, lang), p.Kind.ToString(), p.Category, p.FoodType,
            p.Latitude, p.Longitude, p.Parent?.Slug)).ToList();
    }

    private static bool MatchesCategory(Place p, string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return true;
        if (category.Equals(FoodFilter, StringComparison.OrdinalIgnoreCase)) return p.Kind == PlaceKind.Food;
        return p.Kind == PlaceKind.Destination && string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase);
    }

    private static FoodPlaceSummaryDto ToFood(Place c, string lang) => new(
        c.Id, c.Slug, Localizer.Name(c, lang), c.FoodType, c.CoverImageUrl,
        c.Latitude, c.Longitude, c.Address, c.PriceRange, c.OpeningHours);

    /// <summary>Bo dau + chu thuong de "cho binh tay" khop "Chợ Bình Tây".</summary>
    internal static string Fold(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(ch is 'đ' or 'Đ' ? 'd' : char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}
