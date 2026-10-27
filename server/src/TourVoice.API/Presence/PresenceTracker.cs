using System.Collections.Concurrent;

namespace TourVoice.API.Presence;

public class PresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<string, PresenceEntry> _entries = new();

    public void Heartbeat(string connectionId, string placeSlug)
    {
        _entries[connectionId] = new PresenceEntry(connectionId, placeSlug, DateTime.UtcNow);
    }

    public void Remove(string connectionId)
    {
        _entries.TryRemove(connectionId, out _);
    }

    public int PurgeExpired(TimeSpan ttl)
    {
        var cutoff = DateTime.UtcNow - ttl;
        var expiredKeys = _entries
            .Where(kv => kv.Value.LastSeenUtc <= cutoff)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _entries.TryRemove(key, out _);
        }

        return expiredKeys.Count;
    }

    public Dictionary<string, int> GetCounts()
    {
        return _entries.Values
            .GroupBy(e => e.PlaceSlug)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}