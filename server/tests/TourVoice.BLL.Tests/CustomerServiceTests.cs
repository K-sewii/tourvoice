using Moq;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repoMock = new();
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _service = new CustomerService(_repoMock.Object);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenUserNotFound()
    {
        _repoMock.Setup(r => r.GetUserByIdAsync(99)).ReturnsAsync((User?)null);

        var result = await _service.GetProfileAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsProfile_WhenUserFound()
    {
        var user = new User { Id = 1, Email = "a@b.com", FullName = "Nguyen Van A", Role = UserRole.Customer, PreferredLanguageCode = "vi" };
        _repoMock.Setup(r => r.GetUserByIdAsync(1)).ReturnsAsync(user);

        var result = await _service.GetProfileAsync(1);

        Assert.NotNull(result);
        Assert.Equal("a@b.com", result!.Email);
        Assert.Equal("Customer", result.Role);
    }

    [Fact]
    public async Task GetFavoritesAsync_ReturnsMappedList()
    {
        var favorites = new List<Favorite> { new Favorite { UserId = 1, PlaceId = 5, CreatedAt = DateTime.UtcNow } };
        _repoMock.Setup(r => r.GetFavoritesAsync(1)).ReturnsAsync(favorites);

        var result = await _service.GetFavoritesAsync(1);

        Assert.Single(result);
        Assert.Equal(5, result[0].PlaceId);
    }

    [Fact]
    public async Task AddFavoriteAsync_CallsRepository()
    {
        await _service.AddFavoriteAsync(1, 5);

        _repoMock.Verify(r => r.AddFavoriteAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task RemoveFavoriteAsync_CallsRepository()
    {
        await _service.RemoveFavoriteAsync(1, 5);

        _repoMock.Verify(r => r.RemoveFavoriteAsync(1, 5), Times.Once);
    }

    [Fact]
    public async Task GetVisitsAsync_ReturnsMappedList()
    {
        var visits = new List<Visit> { new Visit { UserId = 1, PlaceId = 7, VisitedAt = DateTime.UtcNow } };
        _repoMock.Setup(r => r.GetVisitsAsync(1)).ReturnsAsync(visits);

        var result = await _service.GetVisitsAsync(1);

        Assert.Single(result);
        Assert.Equal(7, result[0].PlaceId);
    }

    [Fact]
    public async Task MarkVisitedAsync_CallsRepository()
    {
        await _service.MarkVisitedAsync(1, 7);

        _repoMock.Verify(r => r.MarkVisitedAsync(1, 7), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsMappedList()
    {
        var history = new List<ListenEvent>
        {
            new ListenEvent { PlaceId = 3, LanguageCode = "en", ListenedSeconds = 120, PlayedAt = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetHistoryAsync(1)).ReturnsAsync(history);

        var result = await _service.GetHistoryAsync(1);

        Assert.Single(result);
        Assert.Equal("en", result[0].LanguageCode);
        Assert.Equal(120, result[0].ListenedSeconds);
    }
}