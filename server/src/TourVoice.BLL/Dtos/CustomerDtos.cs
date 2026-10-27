namespace TourVoice.BLL.Dtos;

/// <summary>Place ma khach da luu/ghe/nghe; <see cref="ParentSlug"/> co gia tri voi mon (de dung link).</summary>
public record MyPlaceDto(int PlaceId, string Slug, string Name, string Kind, string? ParentSlug);

public record FavoritePlaceDto(MyPlaceDto Place, DateTime CreatedAt);
public record VisitedPlaceDto(MyPlaceDto Place, DateTime VisitedAt);

/// <summary>BR-10: tien do = so diem den da ghe / so diem den Published.</summary>
public record VisitsDto(List<VisitedPlaceDto> Items, int VisitedDestinations, int PublishedDestinations);

public record ListenHistoryDto(MyPlaceDto Place, string LanguageCode, int ListenedSeconds, DateTime PlayedAt);
