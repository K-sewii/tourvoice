namespace TourVoice.API.Presence;

public interface IPresenceTracker
{
    void Heartbeat(string connectionId, string placeSlug);
    void Remove(string connectionId);
    int PurgeExpired(TimeSpan ttl);
    Dictionary<string, int> GetCounts();
}