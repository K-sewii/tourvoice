using TourVoice.DAL.Entities;

namespace TourVoice.BLL.Localization;

/// <summary>
/// BR-01: chon ban dich hien cho khach. Chi ban <c>Reviewed</c> moi hien; ngon ngu yeu cau
/// chua duyet thi tra tieng Viet kem co fallback. Ngon ngu khong ho tro coi nhu tieng Viet.
/// </summary>
public static class Localizer
{
    public const string SourceLanguage = "vi";
    public static readonly IReadOnlyList<string> SupportedLanguages = ["vi", "en", "zh", "ko", "ja"];

    public static string Normalize(string? lang)
    {
        var l = lang?.Trim().ToLowerInvariant();
        return l is not null && SupportedLanguages.Contains(l) ? l : SourceLanguage;
    }

    public readonly record struct Resolved(PlaceTranslation? Translation, string LanguageCode, bool FallbackUsed);

    public static Resolved Resolve(Place place, string lang)
    {
        lang = Normalize(lang);
        var requested = Reviewed(place, lang);
        if (requested is not null) return new(requested, lang, false);

        var vi = Reviewed(place, SourceLanguage);
        return new(vi, SourceLanguage, lang != SourceLanguage);
    }

    /// <summary>Ten hien thi theo BR-01; Place chua co ban nao duoc duyet thi dung slug.</summary>
    public static string Name(Place place, string lang) => Resolve(place, lang).Translation?.Name ?? place.Slug;

    private static PlaceTranslation? Reviewed(Place place, string lang) =>
        place.Translations.FirstOrDefault(t => t.LanguageCode == lang && t.Status == TranslationStatus.Reviewed);
}
