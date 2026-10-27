namespace TourVoice.BLL.Dtos;

/// <summary>Mot ban dich trong Place editor. <see cref="Stale"/> = "ban goc da doi" (BR-05).</summary>
public record AdminTranslationDto(
    string LanguageCode, string Name, string? NarrationText, string Status, string Source,
    bool Stale, decimal? MachineMatch);

/// <summary>Mot dong cua bang cay Places (SCR-A3): diem den co <see cref="Children"/> la cac mon.</summary>
public record AdminPlaceNodeDto(
    int Id, string Slug, string Name, string Kind, string? Category, string? FoodType,
    string Status, int SortOrder, string? CoverImageUrl, bool HasVietnameseNarration,
    int ReviewedLanguages, int DraftLanguages, List<AdminPlaceNodeDto> Children);

public record AdminPlaceDetailDto(
    int Id, string Slug, string Kind, string? Category, string? FoodType,
    int? ParentId, decimal Latitude, decimal Longitude,
    string? CoverImageUrl, string Status, int SortOrder,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null,
    bool SlugLocked = false, string? ParentName = null,
    List<AdminTranslationDto>? Translations = null, List<AdminPlaceNodeDto>? Foods = null);

/// <param name="Name">Ten tieng Viet; bo trong thi tam dung slug.</param>
public record CreatePlaceRequestDto(
    string Slug, string Kind, string? Category, string? FoodType,
    int? ParentId, decimal Latitude, decimal Longitude, int SortOrder,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null,
    string? Name = null);

/// <param name="Slug">Chi doi duoc khi Place con Draft (khoa sau lan Publish dau, BR-08).</param>
public record UpdatePlaceRequestDto(
    string? Category, string? FoodType, decimal Latitude, decimal Longitude,
    string? CoverImageUrl, int SortOrder,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null,
    string? Slug = null);

public record PagedResultDto<T>(List<T> Items, int Total, int Page, int PageSize);
