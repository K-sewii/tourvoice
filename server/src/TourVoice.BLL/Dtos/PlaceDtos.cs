namespace TourVoice.BLL.Dtos;

/// <summary>Diem den trong danh sach (khong co NarrationText - PRD Data model).</summary>
public record PlaceSummaryDto(
    int Id, string Slug, string Name, string Kind, string? Category, string? FoodType,
    string? CoverImageUrl, decimal Latitude, decimal Longitude,
    string? Address, string? PriceRange, string? OpeningHours);

public record FoodPlaceSummaryDto(
    int Id, string Slug, string Name, string? FoodType, string? CoverImageUrl,
    decimal Latitude, decimal Longitude,
    string? Address, string? PriceRange, string? OpeningHours);

/// <summary>
/// Chi tiet Place (SD-01). <see cref="FallbackUsed"/> = true khi ban cua ngon ngu yeu cau chua
/// Reviewed va API tra ban tieng Viet (BR-01). <see cref="IsFavorite"/>/<see cref="IsVisited"/>
/// chi co gia tri khi request kem JWT Customer, nguoc lai la null.
/// </summary>
public record PlaceDetailDto(
    int Id, string Slug, string Name, string? NarrationText,
    string Kind, string? Category, string? FoodType, string? CoverImageUrl,
    decimal Latitude, decimal Longitude,
    string? Address, string? PriceRange, string? OpeningHours,
    string LanguageCode, bool FallbackUsed,
    string? ParentSlug, string? ParentName,
    List<FoodPlaceSummaryDto> FoodPlaces,
    bool? IsFavorite, bool? IsVisited);

/// <summary>Mot muc trong trang Kham pha (SCR-07): diem den hoac mon, kem cha neu la mon.</summary>
public record ExploreItemDto(
    int Id, string Slug, string Name, string Kind, string? Category, string? FoodType,
    string? CoverImageUrl, decimal Latitude, decimal Longitude,
    string? Address, string? PriceRange, string? OpeningHours,
    string? ParentSlug, string? ParentName, int GeofenceRadiusM = 100);

/// <summary>Ghim tren ban do (SCR-04).</summary>
public record MapMarkerDto(
    int Id, string Slug, string Name, string Kind, string? Category, string? FoodType,
    decimal Latitude, decimal Longitude, string? ParentSlug);

/// <summary>Ghi mot luot nghe (BR-09). Nguon luon la giong thiet bi (ADR-02).</summary>
public record ListenEventRequestDto(string PlaceSlug, string LanguageCode, int ListenedSeconds);

public record LanguageDto(string Code, string MtCode, string SpeechLang, bool IsEnabled);

public record UpdateLanguageRequestDto(string MtCode, string SpeechLang, bool IsEnabled);
