using Moq;
using TourVoice.BLL.Presence;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class AdminDashboardServiceTests
{
    // 27/10/2026 08:00 UTC = 15:00 gio Viet Nam, cung ngay.
    private static readonly DateTime Now = new(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IStatsRepository> _stats = new();
    private readonly Mock<IUsageLogRepository> _usage = new();
    private readonly Mock<IPresenceService> _presence = new();
    private readonly AdminDashboardService _service;

    public AdminDashboardServiceTests()
    {
        _service = new AdminDashboardService(_stats.Object, _usage.Object, _presence.Object, new FixedClock(Now));
        _stats.Setup(s => s.GetListenTimesSinceAsync(It.IsAny<DateTime>())).ReturnsAsync([]);
        _stats.Setup(s => s.GetSessionsSinceAsync(It.IsAny<DateTime>())).ReturnsAsync([]);
        _usage.Setup(u => u.GetSinceAsync(UsageService.MyMemory, It.IsAny<DateTime>())).ReturnsAsync([]);
        _presence.Setup(p => p.GetSnapshot()).Returns(Snap(0));
    }

    private sealed class FixedClock(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc, TimeSpan.Zero);
    }

    private static PresenceSnapshotDto Snap(int live) => new(live, new Dictionary<string, int>(), Now);

    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, DateTimeKind.Utc);

    private static AccessSessionSpan Session(DateTime start, DateTime? end, AccessSource source = AccessSource.Web) =>
        new(start, end ?? start, end, source);

    // ---- access stats ----

    [Fact]
    public async Task AccessStats_ReturnsOneRowPerDay_OldestFirst_EndingToday()
    {
        var stats = await _service.GetAccessStatsAsync(14);

        Assert.Equal(14, stats.Days.Count);
        Assert.Equal("2026-10-14", stats.Days[0].Date);
        Assert.Equal("2026-10-27", stats.Days[^1].Date);
    }

    [Fact]
    public async Task AccessStats_ClampsDayCount()
    {
        Assert.Single((await _service.GetAccessStatsAsync(0)).Days);
        Assert.Equal(AdminDashboardService.MaxStatDays, (await _service.GetAccessStatsAsync(10_000)).Days.Count);
    }

    [Fact]
    public async Task AccessStats_BucketsByVietnamLocalDay_NotUtc()
    {
        // 18:00 UTC ngay 26 = 01:00 ngay 27 gio VN -> thuoc ngay 27.
        _stats.Setup(s => s.GetListenTimesSinceAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([At(26, 18), At(26, 16), At(27, 2)]);   // 16:00 UTC = 23:00 ngay 26 VN

        var days = (await _service.GetAccessStatsAsync(2)).Days;

        Assert.Equal(1, days[0].Listens);   // 26/10
        Assert.Equal(2, days[1].Listens);   // 27/10
    }

    [Fact]
    public async Task AccessStats_CountsSessionsQrScansAndPeak()
    {
        _stats.Setup(s => s.GetSessionsSinceAsync(It.IsAny<DateTime>())).ReturnsAsync(
        [
            Session(At(27, 1, 0), At(27, 1, 30), AccessSource.QR),
            Session(At(27, 1, 10), At(27, 1, 40), AccessSource.QR),
            Session(At(27, 1, 20), At(27, 1, 25)),
            Session(At(27, 3, 0), At(27, 3, 5)),
        ]);

        var today = (await _service.GetAccessStatsAsync(1)).Days.Single();

        Assert.Equal(4, today.Sessions);
        Assert.Equal(2, today.QrScans);
        Assert.Equal(3, today.PeakConcurrent);   // 01:20-01:25 co ca ba phien
    }

    // ---- peak ----

    [Fact]
    public void PeakConcurrent_TouchingIntervalsDoNotOverlap()
    {
        var peak = AdminDashboardService.PeakConcurrent(
            [(At(27, 1), At(27, 2)), (At(27, 2), At(27, 3))], At(27, 0), At(28, 0));
        Assert.Equal(1, peak);
    }

    [Fact]
    public void PeakConcurrent_ClipsToDay_AndCountsInstantSession()
    {
        var spansMidnight = (At(26, 23), At(27, 1));
        var instant = (At(27, 5), At(27, 5));
        Assert.Equal(1, AdminDashboardService.PeakConcurrent([spansMidnight], At(27, 0), At(28, 0)));
        Assert.Equal(0, AdminDashboardService.PeakConcurrent([spansMidnight], At(28, 0), At(29, 0)));
        Assert.Equal(1, AdminDashboardService.PeakConcurrent([instant], At(27, 0), At(28, 0)));
    }

    [Fact]
    public async Task AccessStats_OpenSessionEndsOneTtlAfterLastSeen_NotForever()
    {
        // Phien mo tu 3 gio truoc, lan cuoi thay 3 gio truoc (API restart) -> khong tinh la dang mo.
        _stats.Setup(s => s.GetSessionsSinceAsync(It.IsAny<DateTime>())).ReturnsAsync(
        [
            new AccessSessionSpan(At(27, 5), At(27, 5, 1), null, AccessSource.Web),
        ]);

        var today = (await _service.GetAccessStatsAsync(1)).Days.Single();

        Assert.Equal(1, today.PeakConcurrent);
        Assert.Equal(1, today.Sessions);
    }

    // ---- dashboard / usage ----

    [Fact]
    public async Task Dashboard_AssemblesLiveCountsQuotaAndChart()
    {
        _presence.Setup(p => p.GetSnapshot()).Returns(Snap(4));
        _stats.Setup(s => s.CountPublishedPlacesAsync()).ReturnsAsync(16);
        _stats.Setup(s => s.CountDraftTranslationsAsync()).ReturnsAsync(7);
        _usage.Setup(u => u.GetCharacterTotalSinceAsync(UsageService.MyMemory, It.IsAny<DateTime>())).ReturnsAsync(10_000);

        var dash = await _service.GetDashboardAsync();

        Assert.Equal(4, dash.Live);
        Assert.Equal(16, dash.PublishedPlaces);
        Assert.Equal(7, dash.DraftTranslations);
        Assert.Equal(14, dash.Days.Count);
        Assert.Equal(dash.Days[^1], dash.Today);
        Assert.Equal(20.0, dash.Quota.Percent);
        Assert.Equal(45_000, dash.Quota.BlockThreshold);
        Assert.Equal(QuotaPolicy.AverageWindowDays, dash.Quota.AverageDays);
        // UI hien thi chu ky presence tu backend, khong viet cung vao HTML.
        Assert.Equal((int)PresenceService.SweepInterval.TotalSeconds, dash.Presence.RefreshSeconds);
        Assert.Equal((int)PresenceService.Ttl.TotalSeconds, dash.Presence.TtlSeconds);
    }

    [Fact]
    public async Task Usage_ReportsAverageOverSevenDaysAndRecentLogs()
    {
        _usage.Setup(u => u.GetCharacterTotalSinceAsync(UsageService.MyMemory, It.IsAny<DateTime>())).ReturnsAsync(7_000);
        _usage.Setup(u => u.GetSinceAsync(UsageService.MyMemory, At(20, 17))).ReturnsAsync(
        [
            new UsageLog { Characters = 7_000 }, new UsageLog { Characters = 14_000 },
        ]);
        _usage.Setup(u => u.GetRecentAsync(UsageService.MyMemory, 50)).ReturnsAsync(
        [
            new UsageLog { Characters = 500, Chunks = 2, Success = false, ErrorCode = "status_403", CreatedAt = At(27, 6) },
        ]);

        var usage = await _service.GetUsageAsync();

        Assert.Equal(3_000, usage.Quota.Average7Days);   // 21.000 / 7
        Assert.Equal(QuotaPolicy.AverageWindowDays, usage.Quota.AverageDays);
        Assert.Equal(AdminDashboardService.RecentLogCount, usage.RecentLimit);
        // 27/10 08:00 UTC = 15:00 gio VN -> ngay 27 bat dau luc 26/10 17:00 UTC (khong phai 00:00 UTC).
        _usage.Verify(u => u.GetCharacterTotalSinceAsync(UsageService.MyMemory, At(26, 17)), Times.Once);
        var log = Assert.Single(usage.Recent);
        Assert.False(log.Success);
        Assert.Equal("status_403", log.ErrorCode);
    }
}
