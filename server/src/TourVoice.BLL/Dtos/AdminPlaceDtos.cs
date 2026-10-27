namespace TourVoice.BLL.Dtos;

public record AdminPlaceListItemDto(
    int Id, string Slug, string? Category, string Status, int SortOrder,
    bool HasVietnameseNarration);

public record AdminPlaceDetailDto(
    int Id, string Slug, string Kind, string? Category, string? FoodType,
    int? ParentId, decimal Latitude, decimal Longitude,
    string? CoverImageUrl, string Status, int SortOrder,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null);

public record CreatePlaceRequestDto(
    string Slug, string Kind, string? Category, string? FoodType,
    int? ParentId, decimal Latitude, decimal Longitude, int SortOrder,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null);

public record UpdatePlaceRequestDto(
    string? Category, string? FoodType, decimal Latitude, decimal Longitude,
    string? CoverImageUrl, int SortOrder,
    string? Address = null, string? PriceRange = null, string? OpeningHours = null);

public record PagedResultDto<T>(List<T> Items, int Total, int Page, int PageSize);