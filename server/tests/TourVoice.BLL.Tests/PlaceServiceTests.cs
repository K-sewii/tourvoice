using Moq;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class PlaceServiceTests
{
    private static Place MakePlace(params PlaceTranslation[] translations)
    {
        var place = new Place
        {
            Id = 1, Slug = "dinh-binh-tien", Category = "Heritage",
            Latitude = 10.750m, Longitude = 106.642m
        };
        foreach (var t in translations) place.Translations.Add(t);
        return place;
    }

    [Fact]
    public async Task GetPlaceDetailAsync_KhiBanDichDaReviewed_TraDungNgonNguYeuCau()
    {
        var place = MakePlace(new PlaceTranslation
        {
            LanguageCode = "en", Name = "Binh Tien Temple",
            Status = TranslationStatus.Reviewed
        });

        var repo = new Mock<IPlaceRepository>();
        repo.Setup(r => r.GetPublishedBySlugAsync("dinh-binh-tien")).ReturnsAsync(place);
        var service = new PlaceService(repo.Object);

        var result = await service.GetPlaceDetailAsync("dinh-binh-tien", "en");

        Assert.NotNull(result);
        Assert.Equal("en", result!.LanguageCode);
        Assert.False(result.FallbackUsed);
        Assert.Equal("Binh Tien Temple", result.Name);
    }

    [Fact]
    public async Task GetPlaceDetailAsync_KhiBanDichConDraft_FallbackVeTiengViet()
    {
        var place = MakePlace(
            new PlaceTranslation { LanguageCode = "en", Name = "Draft EN", Status = TranslationStatus.Draft },
            new PlaceTranslation { LanguageCode = "vi", Name = "Dinh Binh Tien", Status = TranslationStatus.Reviewed });

        var repo = new Mock<IPlaceRepository>();
        repo.Setup(r => r.GetPublishedBySlugAsync("dinh-binh-tien")).ReturnsAsync(place);
        var service = new PlaceService(repo.Object);

        var result = await service.GetPlaceDetailAsync("dinh-binh-tien", "en");

        Assert.NotNull(result);
        Assert.Equal("vi", result!.LanguageCode);
        Assert.True(result.FallbackUsed);
        Assert.Equal("Dinh Binh Tien", result.Name);
    }

    [Fact]
    public async Task GetPlaceDetailAsync_KhiKhongTonTaiSlug_TraVeNull()
    {
        var repo = new Mock<IPlaceRepository>();
        repo.Setup(r => r.GetPublishedBySlugAsync("khong-ton-tai")).ReturnsAsync((Place?)null);
        var service = new PlaceService(repo.Object);

        var result = await service.GetPlaceDetailAsync("khong-ton-tai", "vi");

        Assert.Null(result);
    }
}