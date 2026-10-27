using System.Globalization;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Presence;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;

namespace TourVoice.BLL.Services;

public class AdminDashboardService : IAdminDashboardService
{
    public const int DashboardDays = 14;
    public const int MaxStatDays = 90;
    public const int RecentLogCount = 50;

    // Viet Nam khong co gio mua he, nen dung offset co dinh +7 thay vi tra cuu mui gio (ICU/tzdata tren container).
    private static readonly TimeSpan LocalOffset = TimeSpan.FromHours(7);

    private readonly IStatsRepository _stats;
    private readonly IUsageLogRepository _usage;
    private readonly IPresenceService _presence;
    private readonly TimeProvider _clock;

    public AdminDashboardService(IStatsRepository stats, IUsageLogRepository usage, IPresenceService presence, TimeProvider clock)
    {
        _stats = stats;
        _usage = usage;
        _presence = presence;
        _clock = clock;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var days = (await GetAccessStatsAsync(DashboardDays)).Days;
        return new DashboardDto(
            _presence.GetSnapshot().Live,
            await _stats.CountPublishedPlacesAsync(),
            await _stats.CountDraftTranslationsAsync(),
            days[^1],
            await BuildQuotaAsync(),
            days);
    }

    public async Task<AccessStatsDto> GetAccessStatsAsync(int days)
    {
        days = Math.Clamp(days, 1, MaxStatDays);
        var nowUtc = _clock.GetUtcNow().UtcDateTime;
        var firstLocal = (nowUtc + LocalOffset).Date.AddDays(-(days - 1));
        var fromUtc = firstLocal - LocalOffset;

        var listens = await _stats.GetListenTimesSinceAsync(fromUtc);
        var sessions = await _stats.GetSessionsSinceAsync(fromUtc);
        // Phien mo ma khong con heartbeat (vd. API khoi dong lai) ket thuc sau TTL ke tu lan cuoi thay.
        var intervals = sessions.Select(s => (
            Start: s.StartedAt,
            End: s.EndedAt ?? Min(nowUtc, s.LastSeenAt + PresenceService.Ttl),
            s.Source)).ToList();

        var result = new List<DayStatDto>();
        for (var i = 0; i < days; i++)
        {
            var dayStartLocal = firstLocal.AddDays(i);
            var dayStart = dayStartLocal - LocalOffset;
            var dayEnd = dayStart.AddDays(1);

            var started = intervals.Where(x => x.Start >= dayStart && x.Start < dayEnd).ToList();
            result.Add(new DayStatDto(
                dayStartLocal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                listens.Count(t => t >= dayStart && t < dayEnd),
                started.Count,
                started.Count(x => x.Source == AccessSource.QR),
                PeakConcurrent(intervals.Select(x => (x.Start, x.End)), dayStart, dayEnd)));
        }
        return new AccessStatsDto(result);
    }

    public async Task<UsageDto> GetUsageAsync()
    {
        var recent = await _usage.GetRecentAsync(UsageService.MyMemory, RecentLogCount);
        return new UsageDto(
            await BuildQuotaAsync(),
            recent.Select(l => new UsageLogDto(l.CreatedAt, l.Characters, l.Chunks, l.Success, l.ErrorCode)).ToList());
    }

    private async Task<QuotaDto> BuildQuotaAsync()
    {
        var used = await _usage.GetTodayCharacterTotalAsync(UsageService.MyMemory);
        // Trung binh 7 ngay UTC gan nhat (gom hom nay), cung truc ngay voi han muc MyMemory.
        var weekStart = _clock.GetUtcNow().UtcDateTime.Date.AddDays(-6);
        var week = await _usage.GetSinceAsync(UsageService.MyMemory, weekStart);
        return new QuotaDto(
            used, QuotaPolicy.DailyLimit, QuotaPolicy.BlockThreshold,
            Math.Round(used * 100.0 / QuotaPolicy.DailyLimit, 1),
            (int)Math.Round(week.Sum(l => (long)l.Characters) / 7.0));
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    /// <summary>So thiet bi dong thoi toi da trong [dayStart, dayEnd) bang quet duong thang qua cac moc vao/ra.</summary>
    internal static int PeakConcurrent(IEnumerable<(DateTime Start, DateTime End)> intervals, DateTime dayStart, DateTime dayEnd)
    {
        var events = new List<(DateTime At, int Delta)>();
        foreach (var (start, rawEnd) in intervals)
        {
            var end = rawEnd <= start ? start.AddSeconds(1) : rawEnd;   // phien tuc thoi van la 1 thiet bi
            var from = start < dayStart ? dayStart : start;
            var to = end > dayEnd ? dayEnd : end;
            if (to <= from) continue;
            events.Add((from, +1));
            events.Add((to, -1));
        }

        // Cung thoi diem: ra truoc, vao sau (khoang ket thuc luc B khong chong khoang bat dau luc B).
        events.Sort((a, b) => a.At != b.At ? a.At.CompareTo(b.At) : a.Delta.CompareTo(b.Delta));
        int current = 0, peak = 0;
        foreach (var (_, delta) in events)
        {
            current += delta;
            if (current > peak) peak = current;
        }
        return peak;
    }
}
