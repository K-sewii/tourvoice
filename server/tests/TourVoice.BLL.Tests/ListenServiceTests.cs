using Moq;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class ListenServiceTests
{
    private readonly Mock<IPlaceRepository> _places = new();
    private readonly Mock<ILanguageRepository> _languages = new();
    private readonly Mock<IListenEventRepository> _events = new();

    public ListenServiceTests()
    {
        _languages.Setup(r => r.GetEnabledAsync()).ReturnsAsync(
        [
            new Language { Code = "vi", MtCode = "vi", SpeechLang = "vi-VN" },
            new Language { Code = "ko", MtCode = "ko", SpeechLang = "ko-KR" },
        ]);
        _places.Setup(r => r.GetPublishedIdBySlugAsync("cho-binh-tay")).ReturnsAsync(2);
    }

    private ListenService Service => new(_places.Object, _languages.Object, _events.Object);

    [Fact]
    public async Task RecordAsync_HopLe_GhiLuotNgheGanTaiKhoan()
    {
        await Service.RecordAsync(new ListenEventRequestDto("cho-binh-tay", "KO", 12), userId: 7);

        _events.Verify(r => r.AddAsync(It.Is<ListenEvent>(e =>
            e.PlaceId == 2 && e.LanguageCode == "ko" && e.ListenedSeconds == 12 && e.UserId == 7)), Times.Once);
    }

    [Fact]
    public async Task RecordAsync_KhachVangLai_UserIdNull()
    {
        await Service.RecordAsync(new ListenEventRequestDto("cho-binh-tay", "vi", 10), userId: null);
        _events.Verify(r => r.AddAsync(It.Is<ListenEvent>(e => e.UserId == null)), Times.Once);
    }

    [Theory]
    [InlineData(9)]      // BR-09: chua du 10 giay
    [InlineData(-5)]
    [InlineData(3601)]
    public async Task RecordAsync_ThoiLuongNgoaiKhoang_Nem400(int seconds)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Service.RecordAsync(new ListenEventRequestDto("cho-binh-tay", "vi", seconds), null));

        Assert.Equal("invalid_duration", ex.Code);
        _events.Verify(r => r.AddAsync(It.IsAny<ListenEvent>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_NgonNguKhongBat_Nem400()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Service.RecordAsync(new ListenEventRequestDto("cho-binh-tay", "fr", 20), null));
        Assert.Equal("invalid_language", ex.Code);
    }

    [Fact]
    public async Task RecordAsync_PlaceKhongPublished_Nem404()
    {
        await Assert.ThrowsAsync<PlaceNotFoundException>(() =>
            Service.RecordAsync(new ListenEventRequestDto("khong-co", "vi", 20), null));
    }
}
