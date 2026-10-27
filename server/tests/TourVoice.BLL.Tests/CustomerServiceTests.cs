using Moq;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;
using static TourVoice.BLL.Tests.TestData;

namespace TourVoice.BLL.Tests;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IPlaceRepository> _places = new();
    private CustomerService Service => new(_customers.Object, _places.Object);

    [Fact]
    public async Task GetProfileAsync_KhongConTaiKhoan_Nem401()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => Service.GetProfileAsync(99));
        Assert.Equal("account_missing", ex.Code);
    }

    [Fact]
    public async Task GetProfileAsync_TraHoSo()
    {
        _customers.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(new User
        {
            Id = 1, Email = "a@test.com", FullName = "A", Role = UserRole.Customer, PreferredLanguageCode = "ko",
        });

        var result = await Service.GetProfileAsync(1);

        Assert.Equal("a@test.com", result.Email);
        Assert.Equal("Customer", result.Role);
        Assert.Equal("ko", result.PreferredLanguageCode);
    }

    [Fact]
    public async Task GetFavoritesAsync_TraSlugTenTheoNgonNgu_VaChaCuaMon()
    {
        var market = Destination(1, "cho-binh-tay", "Market", Tr("vi", "Chợ Bình Tây"), Tr("en", "Binh Tay Market"));
        var food = Food(10, "pho-bo", market, "Noodles", Tr("vi", "Phở bò"));
        var at = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);
        _customers.Setup(r => r.GetFavoritesAsync(1)).ReturnsAsync(
        [
            new Favorite { UserId = 1, PlaceId = 1, Place = market, CreatedAt = at },
            new Favorite { UserId = 1, PlaceId = 10, Place = food, CreatedAt = at },
        ]);

        var result = await Service.GetFavoritesAsync(1, "en");

        Assert.Equal("Binh Tay Market", result[0].Place.Name);
        Assert.Null(result[0].Place.ParentSlug);
        Assert.Equal("pho-bo", result[1].Place.Slug);
        Assert.Equal("cho-binh-tay", result[1].Place.ParentSlug);
        Assert.Equal(at, result[1].CreatedAt);
    }

    [Fact]
    public async Task AddFavoriteAsync_PlaceChuaPublished_Nem404_KhongGhi()
    {
        _places.Setup(r => r.IsPublishedAsync(5)).ReturnsAsync(false);

        await Assert.ThrowsAsync<PlaceNotFoundException>(() => Service.AddFavoriteAsync(1, 5));
        _customers.Verify(r => r.AddFavoriteAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddFavoriteAsync_PlaceHopLe_GhiYeuThich()
    {
        _places.Setup(r => r.IsPublishedAsync(5)).ReturnsAsync(true);

        await Service.AddFavoriteAsync(1, 5);

        _customers.Verify(r => r.AddFavoriteAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_GoiRepository()
    {
        await Service.RemoveFavoriteAsync(1, 5);
        _customers.Verify(r => r.RemoveFavoriteAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task MarkVisitedAsync_PlaceChuaPublished_Nem404()
    {
        await Assert.ThrowsAsync<PlaceNotFoundException>(() => Service.MarkVisitedAsync(1, 5));
        _customers.Verify(r => r.MarkVisitedAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MarkVisitedAsync_PlaceHopLe_GhiDaGhe()
    {
        _places.Setup(r => r.IsPublishedAsync(5)).ReturnsAsync(true);
        await Service.MarkVisitedAsync(1, 5);
        _customers.Verify(r => r.MarkVisitedAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task GetVisitsAsync_TienDoChiDemDiemDenPublished_BR10()
    {
        var market = Destination(1, "cho-binh-tay", "Market", Tr("vi", "Chợ Bình Tây"));
        var hidden = Destination(2, "an", "Park", Tr("vi", "Ẩn"));
        hidden.Status = PlaceStatus.Hidden;
        var food = Food(10, "pho-bo", market, "Noodles", Tr("vi", "Phở bò"));
        _customers.Setup(r => r.GetVisitsAsync(1)).ReturnsAsync(
        [
            new Visit { PlaceId = 1, Place = market },
            new Visit { PlaceId = 2, Place = hidden },
            new Visit { PlaceId = 10, Place = food },
        ]);
        _places.Setup(r => r.CountPublishedDestinationsAsync()).ReturnsAsync(4);

        var result = await Service.GetVisitsAsync(1, "vi");

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.VisitedDestinations);
        Assert.Equal(4, result.PublishedDestinations);
    }

    [Fact]
    public async Task GetHistoryAsync_TraPlaceVaThoiLuong()
    {
        var market = Destination(1, "cho-binh-tay", "Market", Tr("vi", "Chợ Bình Tây"));
        _customers.Setup(r => r.GetHistoryAsync(1)).ReturnsAsync(
        [
            new ListenEvent { PlaceId = 1, Place = market, LanguageCode = "ko", ListenedSeconds = 42 },
        ]);

        var item = Assert.Single(await Service.GetHistoryAsync(1, "vi"));

        Assert.Equal("cho-binh-tay", item.Place.Slug);
        Assert.Equal("ko", item.LanguageCode);
        Assert.Equal(42, item.ListenedSeconds);
    }
}
