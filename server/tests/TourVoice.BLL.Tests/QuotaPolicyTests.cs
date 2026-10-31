using TourVoice.BLL.Services;
using Xunit;

namespace TourVoice.BLL.Tests;

public class QuotaPolicyTests
{
    private sealed class FixedClock(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utc, TimeSpan.Zero);
    }

    [Theory]
    // 18:30 UTC ngay 27 = 01:30 ngay 28 gio VN -> ngay 28 bat dau luc 17:00 UTC ngay 27.
    [InlineData(2026, 10, 27, 18, 30, 2026, 10, 27, 17)]
    // 16:59 UTC ngay 27 = 23:59 ngay 27 gio VN -> van la ngay 27, bat dau luc 17:00 UTC ngay 26.
    [InlineData(2026, 10, 27, 16, 59, 2026, 10, 26, 17)]
    // dung 17:00 UTC = 00:00 ngay 28 gio VN: da sang ngay moi.
    [InlineData(2026, 10, 27, 17, 0, 2026, 10, 27, 17)]
    public void DayStartUtc_IsMidnightInVietnam(
        int y, int m, int d, int h, int min, int ey, int em, int ed, int eh)
    {
        var clock = new FixedClock(new DateTime(y, m, d, h, min, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(ey, em, ed, eh, 0, 0, DateTimeKind.Utc), QuotaPolicy.DayStartUtc(clock));
    }
}
