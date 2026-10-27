using TourVoice.DAL.Entities;

namespace TourVoice.BLL.Tests;

/// <summary>Dung entity mau cho test BLL.</summary>
internal static class TestData
{
    public static PlaceTranslation Tr(string lang, string name, TranslationStatus status = TranslationStatus.Reviewed,
        string? narration = null) =>
        new() { LanguageCode = lang, Name = name, Status = status, NarrationText = narration ?? $"{name}. Câu hai." };

    public static Place Destination(int id, string slug, string category = "Market", params PlaceTranslation[] translations)
    {
        var p = new Place
        {
            Id = id, Slug = slug, Kind = PlaceKind.Destination, Category = category,
            Latitude = 10.749m, Longitude = 106.651m, Status = PlaceStatus.Published, Address = "57A Tháp Mười",
        };
        foreach (var t in translations) p.Translations.Add(t);
        return p;
    }

    public static Place Food(int id, string slug, Place parent, string foodType = "Noodles", params PlaceTranslation[] translations)
    {
        var p = new Place
        {
            Id = id, Slug = slug, Kind = PlaceKind.Food, FoodType = foodType, Parent = parent, ParentId = parent.Id,
            Latitude = 10.7496m, Longitude = 106.6516m, Status = PlaceStatus.Published, PriceRange = "40.000đ",
        };
        foreach (var t in translations) p.Translations.Add(t);
        parent.Children.Add(p);
        return p;
    }
}
