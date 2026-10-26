namespace TourVoice.BLL.Dtos;

public record PlaceSummaryDto(
    int Id, string Slug, string Name, string? Category,
    string? CoverImageUrl, decimal Latitude, decimal Longitude);

public record PlaceDetailDto(
    int Id, string Slug, string Name, string? NarrationText,
    string? AudioUrl, int? AudioDurationSeconds,
    string? Category, decimal Latitude, decimal Longitude,
    string LanguageCode, bool FallbackUsed);

public record LanguageDto(string Code, string VoiceName, bool IsEnabled);