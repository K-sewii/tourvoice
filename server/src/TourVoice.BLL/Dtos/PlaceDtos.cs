namespace TourVoice.BLL.Dtos;

public record PlaceSummaryDto(
    int Id, string Slug, string Name, string? Category,
    string? CoverImageUrl, decimal Latitude, decimal Longitude,
    string Kind = "", string? FoodType = null,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null);

public record FoodPlaceSummaryDto(
    int Id, string Slug, string Name, string? FoodType,
    string? CoverImageUrl, string? Address, string? PriceRange, string? OpeningHours);

public record PlaceDetailDto(
    int Id, string Slug, string Name, string? NarrationText,
    string? AudioUrl, int? AudioDurationSeconds,
    string? Category, decimal Latitude, decimal Longitude,
    string LanguageCode, bool FallbackUsed,
    string Kind = "", string? FoodType = null,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null,
    List<FoodPlaceSummaryDto>? FoodPlaces = null);

public record RecordListenRequestDto(string LanguageCode, int ListenedSeconds, string Source = "Mp3");

public record LanguageDto(string Code, string VoiceName, bool IsEnabled);