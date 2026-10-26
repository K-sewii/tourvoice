namespace TourVoice.DAL.Entities;

public class PlaceTranslation
{
    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;
    public string LanguageCode { get; set; } = null!;     // PK ghep: (PlaceId, LanguageCode)
    public Language Language { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? NarrationText { get; set; }             // toi da 4000 ky tu (BR-04)
    public TranslationStatus Status { get; set; } = TranslationStatus.Draft;
    public TranslationSource Source { get; set; } = TranslationSource.Human;

    public string? AudioUrl { get; set; }
    public int? AudioDurationSeconds { get; set; }
    public string? AudioHash { get; set; }                  // hash(van ban + giong) - tai su dung (BR-05)
}