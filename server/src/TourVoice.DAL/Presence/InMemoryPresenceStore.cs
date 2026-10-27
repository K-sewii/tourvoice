using System.Collections.Concurrent;
using TourVoice.DAL.Entities;

namespace TourVoice.DAL.Presence;

/// <summary>Mot thiet bi dang mo trang. DeviceKey la SHA-256 cua deviceId (BR-14), khong luu id goc.</summary>
public record PresenceEntry(string DeviceKey, string? PlaceSlug, string Lang, AccessSource Source,
    long SessionId, DateTime LastSeenUtc);

/// <summary>
/// So thiet bi dang truy cap song trong bo nho tien trinh (ADR-10): heartbeat khong ghi SQL.
/// Mot container API nen bo nho chung la nhat quan; muon scale ngang thi doi sang Redis.
/// </summary>
public interface IPresenceStore
{
    PresenceEntry? Get(string deviceKey);

    /// <summary>Them thiet bi moi; false neu da co (heartbeat dong thoi cua cung mot may).</summary>
    bool TryAdd(PresenceEntry entry);

    /// <summary>
    /// Lam moi thiet bi da co (trang, ngon ngu, LastSeen) mot cach nguyen tu, giu nguyen SessionId
    /// hien tai. Tra ve ban ghi truoc khi cap nhat, hoac null neu thiet bi khong con.
    /// </summary>
    PresenceEntry? Touch(string deviceKey, string? placeSlug, string lang, DateTime nowUtc);

    /// <summary>Gan SessionId cho thiet bi neu no van con; false neu da roi di.</summary>
    bool SetSession(string deviceKey, long sessionId);

    PresenceEntry? Remove(string deviceKey);

    /// <summary>Xoa va tra ve cac thiet bi im lang tu truoc <paramref name="cutoffUtc"/>.</summary>
    IReadOnlyList<PresenceEntry> RemoveExpired(DateTime cutoffUtc);

    IReadOnlyCollection<PresenceEntry> All();
}

public class InMemoryPresenceStore : IPresenceStore
{
    private readonly ConcurrentDictionary<string, PresenceEntry> _entries = new();

    public PresenceEntry? Get(string deviceKey) => _entries.GetValueOrDefault(deviceKey);

    public bool TryAdd(PresenceEntry entry) => _entries.TryAdd(entry.DeviceKey, entry);

    // Ca hai deu doc gia tri moi nhat va TryUpdate theo dung gia tri do: hai heartbeat dong thoi
    // (StrictMode, nhieu tab) khong the ghi de SessionId bang mot ban chup cu.
    public PresenceEntry? Touch(string deviceKey, string? placeSlug, string lang, DateTime nowUtc)
    {
        while (_entries.TryGetValue(deviceKey, out var current))
        {
            var next = current with { PlaceSlug = placeSlug, Lang = lang, LastSeenUtc = nowUtc };
            if (_entries.TryUpdate(deviceKey, next, current)) return current;
        }
        return null;
    }

    public bool SetSession(string deviceKey, long sessionId)
    {
        while (_entries.TryGetValue(deviceKey, out var current))
        {
            if (_entries.TryUpdate(deviceKey, current with { SessionId = sessionId }, current)) return true;
        }
        return false;
    }

    public PresenceEntry? Remove(string deviceKey) => _entries.TryRemove(deviceKey, out var e) ? e : null;

    public IReadOnlyList<PresenceEntry> RemoveExpired(DateTime cutoffUtc)
    {
        var removed = new List<PresenceEntry>();
        foreach (var (key, entry) in _entries)
        {
            // TryRemove theo cap (key, value): heartbeat moi den giua chung thi khong bi xoa nham.
            if (entry.LastSeenUtc <= cutoffUtc && _entries.TryRemove(new KeyValuePair<string, PresenceEntry>(key, entry)))
                removed.Add(entry);
        }
        return removed;
    }

    public IReadOnlyCollection<PresenceEntry> All() => _entries.Values.ToList();
}
