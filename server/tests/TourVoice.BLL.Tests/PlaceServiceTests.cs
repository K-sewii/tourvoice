using Moq;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;
using static TourVoice.BLL.Tests.TestData;

namespace TourVoice.BLL.Tests;

public class PlaceServiceTests
{
    private readonly Mock<IPlaceRepository> _places = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private PlaceService Service => new(_places.Object, _customers.Object);

    private Place SeedMarket()
    {
        var market = Destination(1, "cho-binh-tay", "Market",
            Tr("vi", "Chợ Bình Tây"), Tr("en", "Binh Tay Market"), Tr("ko", "빈떠이 시장", TranslationStatus.Draft));
        Food(10, "pho-bo", market, "Noodles", Tr("vi", "[Mẫu] Phở bò"));
        return market;
    }

    [Fact]
    public async Task GetPlaceDetailAsync_BanDichDaReviewed_TraDungNgonNgu_KhongFallback()
    {
        _places.Setup(r => r.GetPublishedBySlugAsync("cho-binh-tay")).ReturnsAsync(SeedMarket());

        var result = await Service.GetPlaceDetailAsync("cho-binh-tay", "en");

        Assert.Equal("Binh Tay Market", result.Name);
        Assert.Equal("en", result.LanguageCode);
        Assert.False(result.FallbackUsed);
        Assert.Single(result.FoodPlaces);
        Assert.Null(result.IsFavorite);   // khong co JWT Customer
    }

    [Fact]
    public async Task GetPlaceDetailAsync_BanDichConDraft_FallbackTiengViet_BR01()
    {
        _places.Setup(r => r.GetPublishedBySlugAsync("cho-binh-tay")).ReturnsAsync(SeedMarket());

        var result = await Service.GetPlaceDetailAsync("cho-binh-tay", "ko");

        Assert.Equal("Chợ Bình Tây", result.Name);
        Assert.Equal("vi", result.LanguageCode);
        Assert.True(result.FallbackUsed);
    }

    [Fact]
    public async Task GetPlaceDetailAsync_YeuCauTiengViet_KhongBaoFallback()
    {
        _places.Setup(r => r.GetPublishedBySlugAsync("cho-binh-tay")).ReturnsAsync(SeedMarket());

        var result = await Service.GetPlaceDetailAsync("cho-binh-tay", "vi");

        Assert.False(result.FallbackUsed);
    }

    [Fact]
    public async Task GetPlaceDetailAsync_Mon_TraCha_VaTrangThaiCuaKhach()
    {
        var market = SeedMarket();
        _places.Setup(r => r.GetPublishedBySlugAsync("pho-bo")).ReturnsAsync(market.Children.Single());
        _customers.Setup(r => r.IsFavoriteAsync(7, 10)).ReturnsAsync(true);
        _customers.Setup(r => r.IsVisitedAsync(7, 10)).ReturnsAsync(false);

        var result = await Service.GetPlaceDetailAsync("pho-bo", "en", customerId: 7);

        Assert.Equal("cho-binh-tay", result.ParentSlug);
        Assert.Equal("Binh Tay Market", result.ParentName);
        Assert.Equal("[Mẫu] Phở bò", result.Name);   // mon chua co ban en -> vi
        Assert.True(result.FallbackUsed);
        Assert.True(result.IsFavorite);
        Assert.False(result.IsVisited);
    }

    [Fact]
    public async Task GetPlaceDetailAsync_SlugKhongTonTai_Nem404()
    {
        await Assert.ThrowsAsync<PlaceNotFoundException>(() => Service.GetPlaceDetailAsync("khong-co", "vi"));
    }

    [Fact]
    public async Task GetPlaceDetailAsync_ChuaCoBanViDuocDuyet_Nem404()
    {
        var place = Destination(2, "moi", "Park", Tr("vi", "Mới", TranslationStatus.Draft));
        _places.Setup(r => r.GetPublishedBySlugAsync("moi")).ReturnsAsync(place);

        await Assert.ThrowsAsync<PlaceNotFoundException>(() => Service.GetPlaceDetailAsync("moi", "vi"));
    }

    [Fact]
    public async Task GetFoodsOfDestinationAsync_TraMonCon_HoacNem404VoiMon()
    {
        var market = SeedMarket();
        _places.Setup(r => r.GetPublishedBySlugAsync("cho-binh-tay")).ReturnsAsync(market);
        _places.Setup(r => r.GetPublishedBySlugAsync("pho-bo")).ReturnsAsync(market.Children.Single());

        var foods = await Service.GetFoodsOfDestinationAsync("cho-binh-tay", "vi");

        Assert.Equal("pho-bo", Assert.Single(foods).Slug);
        await Assert.ThrowsAsync<PlaceNotFoundException>(() => Service.GetFoodsOfDestinationAsync("pho-bo", "vi"));
    }

    [Fact]
    public async Task GetPublishedDestinationsAsync_DungTenTheoNgonNgu()
    {
        _places.Setup(r => r.GetPublishedDestinationsAsync()).ReturnsAsync([SeedMarket()]);

        var result = await Service.GetPublishedDestinationsAsync("en");

        Assert.Equal("Binh Tay Market", Assert.Single(result).Name);
        Assert.Equal("Destination", result[0].Kind);
    }

    [Theory]
    [InlineData(null, null, 2)]
    [InlineData("food", null, 1)]
    [InlineData("Market", null, 1)]
    [InlineData("Heritage", null, 0)]
    [InlineData(null, "cho binh tay", 2)]   // khong dau van khop; mon khop qua ten cha
    [InlineData(null, "PHỞ", 1)]
    [InlineData(null, "khong co gi", 0)]
    public async Task ExploreAsync_LocTheoDanhMucVaTuKhoa(string? category, string? q, int expected)
    {
        var market = SeedMarket();
        _places.Setup(r => r.GetPublishedCatalogAsync()).ReturnsAsync([market, market.Children.Single()]);

        var result = await Service.ExploreAsync("vi", category, q);

        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public async Task ExploreAsync_MonCoThongTinCha()
    {
        var market = SeedMarket();
        _places.Setup(r => r.GetPublishedCatalogAsync()).ReturnsAsync([market, market.Children.Single()]);

        var food = (await Service.ExploreAsync("en", "food", null)).Single();

        Assert.Equal("cho-binh-tay", food.ParentSlug);
        Assert.Equal("Binh Tay Market", food.ParentName);
    }

    [Fact]
    public async Task GetMapMarkersAsync_TraToaDoTungPlace()
    {
        var market = SeedMarket();
        _places.Setup(r => r.GetPublishedCatalogAsync()).ReturnsAsync([market, market.Children.Single()]);

        var markers = await Service.GetMapMarkersAsync("vi");

        Assert.Equal(2, markers.Count);
        Assert.Equal(10.7496m, markers[1].Latitude);
        Assert.Equal("cho-binh-tay", markers[1].ParentSlug);
    }
}
