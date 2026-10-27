namespace TourVoice.API.Presence;

public record PresenceEntry(string ConnectionId, string PlaceSlug, DateTime LastSeenUtc);