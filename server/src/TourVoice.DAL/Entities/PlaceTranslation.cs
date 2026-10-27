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
    public TranslationSource Source { get; set; } = TranslationSource.Manual;

    public string? SourceHash { get; set; }                // SHA-256 cua ban vi da duyet luc dich (BR-05)
    public decimal? MachineMatch { get; set; }             // diem match MyMemory tra ve (0..1)
}
