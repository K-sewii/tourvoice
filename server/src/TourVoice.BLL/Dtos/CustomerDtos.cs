namespace TourVoice.BLL.Dtos;

public record FavoritePlaceDto(int PlaceId, string Slug, string Name, DateTime CreatedAt);
public record VisitedPlaceDto(int PlaceId, string Slug, string Name, DateTime VisitedAt);
public record ListenHistoryDto(int PlaceId, string LanguageCode, int ListenedSeconds, DateTime PlayedAt);