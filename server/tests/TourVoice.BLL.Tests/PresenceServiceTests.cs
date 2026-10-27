using Moq;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Presence;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Presence;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

/// <summary>SD-04 / BR-14..17 voi store that (InMemoryPresenceStore) va dong ho gia.</summary>
public class PresenceServiceTests
{
    private const string Device = "6f1c2a9e-4b7d-4c1e-9a55-0d2e8f3b7c11";

    private readonly InMemoryPresenceStore _store = new();
    private readonly Mock<IAccessSessionRepository> _sessions = new();
    private readonly Mock<IPlaceRepository> _places = new();
    private readonly List<PresenceSnapshotDto> _notified = [];
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 27, 8, 0, 0, TimeSpan.Zero));
    private readonly PresenceService _service;

    public PresenceServiceTests()
    {
        var notifier = new Mock<IPresenceNotifier>();
        notifier.Setup(n => n.NotifyAsync(It.IsAny<PresenceSnapshotDto>(), It.IsAny<CancellationToken>()))
            .Callback<PresenceSnapshotDto, CancellationToken>((s, _) => _notified.Add(s))
            .Returns(Task.CompletedTask);
        _sessions.Setup(r => r.AddAsync(It.IsAny<AccessSession>())).ReturnsAsync(100);
        _places.Setup(r => r.GetPublishedIdBySlugAsync("cho-binh-tay")).ReturnsAsync(2);
        _service = new PresenceService(_store, _sessions.Object, _places.Object, notifier.Object, _clock);
    }

    private Task Beat(string device = Device, string? slug = "cho-binh-tay", string source = "web") =>
        _service.HeartbeatAsync(device, new HeartbeatRequestDto(slug, "ko", source));

    [Fact]
    public async Task Heartbeat_ThietBiMoi_MoPhien_BaoDashboard_KhongLuuDeviceIdGoc()
    {
        await Beat(source: "qr");

        Assert.Equal(1, _service.GetSnapshot().Live);
        Assert.Equal(1, _service.GetSnapshot().ByPlace["cho-binh-tay"]);
        var entry = Assert.Single(_store.All());
        Assert.Equal(64, entry.DeviceKey.Length);                  // BR-14: SHA-256 hex
        Assert.DoesNotContain(Device, entry.DeviceKey);
        Assert.Equal(100, entry.SessionId);
        _sessions.Verify(r => r.AddAsync(It.Is<AccessSession>(a =>
            a.Source == AccessSource.QR && a.EntryPlaceId == 2 && a.DeviceKey == entry.DeviceKey)), Times.Once);
        Assert.Single(_notified);
    }

    [Fact]
    public async Task Heartbeat_LapLaiCungTrang_KhongGhiCsdl_KhongBaoLai()
    {
        await Beat();
        _clock.Advance(TimeSpan.FromSeconds(15));
        await Beat();

        _sessions.Verify(r => r.AddAsync(It.IsAny<AccessSession>()), Times.Once);
        Assert.Single(_notified);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, Assert.Single(_store.All()).LastSeenUtc);
    }

    [Fact]
    public async Task Heartbeat_ChuyenTrang_BaoDashboard()
    {
        await Beat();
        await Beat(slug: null);

        Assert.Equal(2, _notified.Count);
        Assert.Empty(_notified[1].ByPlace);
        Assert.Equal(1, _notified[1].Live);
    }

    [Fact]
    public async Task Heartbeat_QuayLaiTrong30Phut_MoLaiPhienCu_BR16()
    {
        var key = PresenceService.DeviceKey(Device);
        _sessions.Setup(r => r.GetLatestAsync(key)).ReturnsAsync(new AccessSession
        {
            Id = 55, DeviceKey = key, EndedAt = _clock.GetUtcNow().UtcDateTime.AddMinutes(-29),
        });

        await Beat();

        _sessions.Verify(r => r.ReopenAsync(55, It.IsAny<DateTime>()), Times.Once);
        _sessions.Verify(r => r.AddAsync(It.IsAny<AccessSession>()), Times.Never);
        Assert.Equal(55, _store.All().Single().SessionId);
    }

    [Fact]
    public async Task Heartbeat_VangQua30Phut_MoPhienMoi_BR16()
    {
        var key = PresenceService.DeviceKey(Device);
        _sessions.Setup(r => r.GetLatestAsync(key)).ReturnsAsync(new AccessSession
        {
            Id = 55, DeviceKey = key, EndedAt = _clock.GetUtcNow().UtcDateTime.AddMinutes(-31),
        });

        await Beat();

        _sessions.Verify(r => r.ReopenAsync(It.IsAny<long>(), It.IsAny<DateTime>()), Times.Never);
        _sessions.Verify(r => r.AddAsync(It.IsAny<AccessSession>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ngan")]
    [InlineData("<script>alert(1)</script>")]
    public async Task Heartbeat_DeviceIdSai_Nem400(string? deviceId)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => Beat(deviceId!));
        Assert.Equal("invalid_device_id", ex.Code);
    }

    [Fact]
    public async Task Leave_DongPhien_GiamSoNgay()
    {
        await Beat();
        await _service.LeaveAsync(Device);

        Assert.Equal(0, _service.GetSnapshot().Live);
        _sessions.Verify(r => r.EndAsync(100, It.IsAny<DateTime>()), Times.Once);
        Assert.Equal(0, _notified.Last().Live);
    }

    [Fact]
    public async Task Leave_ThietBiLaHoacSai_KhongLamGi()
    {
        await _service.LeaveAsync(Device);
        await _service.LeaveAsync("x");

        Assert.Empty(_notified);
        _sessions.Verify(r => r.EndAsync(It.IsAny<long>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Sweep_LoaiThietBiImLangQua45Giay_BR15()
    {
        await Beat();
        await Beat("a1b2c3d4-0000-4000-8000-000000000002");
        _clock.Advance(TimeSpan.FromSeconds(30));
        await Beat();                                   // thiet bi 1 con song
        _clock.Advance(TimeSpan.FromSeconds(16));       // thiet bi 2 im lang 46 giay

        await _service.SweepAsync();

        Assert.Equal(1, _service.GetSnapshot().Live);
        _sessions.Verify(r => r.EndAsync(100, It.IsAny<DateTime>()), Times.Once);
        Assert.Equal(1, _notified.Last().Live);
    }

    [Fact]
    public async Task Sweep_KhongAiHetHan_KhongBao()
    {
        await Beat();
        _notified.Clear();

        await _service.SweepAsync();

        Assert.Empty(_notified);
    }

    [Fact]
    public async Task BonThietBiCungQuetMotQr_DemDu4()
    {
        var devices = Enumerable.Range(1, 4).Select(i => $"00000000-0000-4000-8000-00000000000{i}").ToArray();
        await Task.WhenAll(devices.Select(d => Beat(d, source: "qr")));

        Assert.Equal(4, _service.GetSnapshot().Live);
        Assert.Equal(4, _service.GetSnapshot().ByPlace["cho-binh-tay"]);
    }

    [Fact]
    public void Store_TouchGiuSessionId_SetSessionKhongHoiSinhThietBiDaRoi()
    {
        var key = PresenceService.DeviceKey(Device);
        _store.TryAdd(new PresenceEntry(key, null, "vi", AccessSource.Web, 0, DateTime.UtcNow));
        Assert.True(_store.SetSession(key, 7));
        _store.Touch(key, "cho-binh-tay", "ko", DateTime.UtcNow);
        Assert.Equal(7, _store.Get(key)!.SessionId);

        _store.Remove(key);
        Assert.False(_store.SetSession(key, 8));
        Assert.Null(_store.Get(key));
    }

    [Fact]
    public async Task HeartbeatDongThoiCuaMotMay_KhongMatSessionId_LeaveVanDongPhien()
    {
        // Loi that tim thay khi chay voi MySQL: heartbeat thu hai ghi de SessionId bang ban chup cu,
        // nen leave khong dong duoc AccessSession. Chay song song nhieu vong de bat race.
        for (var round = 0; round < 50; round++)
        {
            var store = new InMemoryPresenceStore();
            var sessions = new Mock<IAccessSessionRepository>();
            sessions.Setup(r => r.AddAsync(It.IsAny<AccessSession>()))
                .Returns(async () => { await Task.Yield(); return 100L; });
            var service = new PresenceService(store, sessions.Object, _places.Object,
                Mock.Of<IPresenceNotifier>(), _clock);

            await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
                service.HeartbeatAsync(Device, new HeartbeatRequestDto("cho-binh-tay", "vi", "web")))));
            await service.LeaveAsync(Device);

            sessions.Verify(r => r.AddAsync(It.IsAny<AccessSession>()), Times.Once);
            sessions.Verify(r => r.EndAsync(100, It.IsAny<DateTime>()), Times.Once);
        }
    }

    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
