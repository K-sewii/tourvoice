using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TourVoice.BLL.Exceptions;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Presence;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Presence;

public record HeartbeatRequestDto(string? PlaceSlug, string? Lang, string? Source);

/// <summary>So lieu dashboard nhan: tong thiet bi dang mo trang va so theo tung Place (khong bao gio co deviceId).</summary>
public record PresenceSnapshotDto(int Live, Dictionary<string, int> ByPlace, DateTime AtUtc);

/// <summary>Interface o BLL, cai dat o Presentation bang IHubContext (SignalR) - BLL khong biet SignalR.</summary>
public interface IPresenceNotifier
{
    Task NotifyAsync(PresenceSnapshotDto snapshot, CancellationToken ct = default);
}

public interface IPresenceService
{
    /// <exception cref="ValidationException">deviceId khong hop le.</exception>
    Task HeartbeatAsync(string? deviceId, HeartbeatRequestDto request, CancellationToken ct = default);
    Task LeaveAsync(string? deviceId, CancellationToken ct = default);
    /// <summary>Loai thiet bi im lang qua TTL, dong phien (PresenceSweeper goi moi 5 giay).</summary>
    Task SweepAsync(CancellationToken ct = default);
    PresenceSnapshotDto GetSnapshot();
    /// <summary>Dem so thiet bi dang online (last seen trong TTL) de hien thi tren dashboard/ma</summary>
    Task<int> GetLiveDeviceCountAsync(DateTimeOffset referenceTimeUtc);
}

/// <summary>
/// Dem nguoi truy cap (SD-04, BR-14..17): heartbeat 15 s lam moi TTL 45 s trong bo nho;
/// thiet bi moi mo phien AccessSession (quay lai trong 30 phut thi mo lai phien cu);
/// moi thay doi so lieu deu day toi dashboard qua <see cref="IPresenceNotifier"/>.
/// </summary>
public partial class PresenceService : IPresenceService
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(45);              // BR-15
    public static readonly TimeSpan NewSessionAfter = TimeSpan.FromMinutes(30);  // BR-16
    /// <summary>PresenceSweeper quet lai moi 15 giay; dashboard admin hien thi chu ky nay tu backend.</summary>
    public static readonly TimeSpan SweepInterval = TimeSpan.FromSeconds(15);

    private readonly IPresenceStore _store;
    private readonly IAccessSessionRepository _sessions;
    private readonly IPlaceRepository _places;
    private readonly IPresenceNotifier _notifier;
    private readonly TimeProvider _clock;

    public PresenceService(IPresenceStore store, IAccessSessionRepository sessions, IPlaceRepository places,
        IPresenceNotifier notifier, TimeProvider clock)
    {
        _store = store;
        _sessions = sessions;
        _places = places;
        _notifier = notifier;
        _clock = clock;
    }

    public async Task HeartbeatAsync(string? deviceId, HeartbeatRequestDto request, CancellationToken ct = default)
    {
        var key = DeviceKey(deviceId);
        var now = _clock.GetUtcNow().UtcDateTime;
        var slug = string.IsNullOrWhiteSpace(request.PlaceSlug) ? null : request.PlaceSlug.Trim();
        var lang = Localization.Localizer.Normalize(request.Lang);

        var previous = _store.Touch(key, slug, lang, now);
        if (previous is not null)
        {
            if (previous.PlaceSlug != slug) await NotifyAsync(ct);   // chuyen trang -> so theo Place doi
            return;
        }

        var source = string.Equals(request.Source, "qr", StringComparison.OrdinalIgnoreCase) ? AccessSource.QR : AccessSource.Web;
        var entry = new PresenceEntry(key, slug, lang, source, SessionId: 0, now);
        if (!_store.TryAdd(entry))
        {
            _store.Touch(key, slug, lang, now);   // heartbeat dong thoi cua cung may vua them truoc
            return;
        }

        var sessionId = await OpenSessionAsync(key, source, slug, now);
        // Thiet bi co the da roi (leave) trong luc dang ghi CSDL - khi do dong phien ngay, khong them lai.
        if (!_store.SetSession(key, sessionId))
            await _sessions.EndAsync(sessionId, _clock.GetUtcNow().UtcDateTime);
        await NotifyAsync(ct);
    }

    public async Task LeaveAsync(string? deviceId, CancellationToken ct = default)
    {
        if (!IsValidDeviceId(deviceId)) return;   // sendBeacon: khong co ai doc loi
        var entry = _store.Remove(DeviceKey(deviceId));
        if (entry is null) return;

        await EndSessionAsync(entry, _clock.GetUtcNow().UtcDateTime);
        await NotifyAsync(ct);
    }

    public async Task SweepAsync(CancellationToken ct = default)
    {
        var expired = _store.RemoveExpired(_clock.GetUtcNow().UtcDateTime - Ttl);
        if (expired.Count == 0) return;

        foreach (var entry in expired) await EndSessionAsync(entry, entry.LastSeenUtc);
        await NotifyAsync(ct);
    }

    public PresenceSnapshotDto GetSnapshot()
    {
        var all = _store.All();
        var byPlace = all.Where(e => e.PlaceSlug is not null)
            .GroupBy(e => e.PlaceSlug!)
            .ToDictionary(g => g.Key, g => g.Count());
        return new PresenceSnapshotDto(all.Count, byPlace, _clock.GetUtcNow().UtcDateTime);
    }

    private async Task<long> OpenSessionAsync(string key, AccessSource source, string? slug, DateTime now)
    {
        var latest = await _sessions.GetLatestAsync(key);
        if (latest is not null && (latest.EndedAt is null || now - latest.EndedAt.Value <= NewSessionAfter))
        {
            await _sessions.ReopenAsync(latest.Id, now);
            return latest.Id;
        }

        return await _sessions.AddAsync(new AccessSession
        {
            DeviceKey = key,
            Source = source,
            EntryPlaceId = slug is null ? null : await _places.GetPublishedIdBySlugAsync(slug),
            StartedAt = now,
            LastSeenAt = now,
        });
    }

    /// <summary>Dem so thiet bi dang online (last seen trong TTL) de hien thi tren dashboard/ma</summary>
    public async Task<int> GetLiveDeviceCountAsync(DateTimeOffset referenceTimeUtc)
    {
        var cutoff = referenceTimeUtc - Ttl;
        var all = _store.All().Where(e => e.LastSeenUtc >= cutoff);
        return all.Count();
    }

    private Task EndSessionAsync(PresenceEntry entry, DateTime endedUtc) =>
        entry.SessionId == 0 ? Task.CompletedTask : _sessions.EndAsync(entry.SessionId, endedUtc);

    private Task NotifyAsync(CancellationToken ct) => _notifier.NotifyAsync(GetSnapshot(), ct);

    /// <summary>BR-14: may chu chi giu SHA-256 cua deviceId.</summary>
    public static string DeviceKey(string? deviceId)
    {
        if (!IsValidDeviceId(deviceId))
            throw new ValidationException("invalid_device_id", "Thiếu hoặc sai X-Device-Id.");
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(deviceId!)));
    }

    internal static bool IsValidDeviceId(string? deviceId) => deviceId is not null && DeviceIdPattern().IsMatch(deviceId);

    // UUID v4 tu crypto.randomUUID() - chap nhan chuoi 8..64 ky tu an toan.
    [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
    private static partial Regex DeviceIdPattern();
}
