using Moq;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class AdminPlaceServiceTests
{
    private readonly Mock<IPlaceRepository> _repoMock = new();
    private readonly AdminPlaceService _service;

    public AdminPlaceServiceTests()
    {
        _service = new AdminPlaceService(_repoMock.Object);
    }

    [Fact]
    public async Task GetListAsync_ComputesHasVietnameseNarration_FromTranslations()
    {
        var places = new List<Place>
        {
            new Place
            {
                Id = 1, Slug = "dinh-binh-tien", Category = "Heritage", SortOrder = 1, Status = PlaceStatus.Published,
                Translations = new List<PlaceTranslation>
                {
                    new PlaceTranslation { LanguageCode = "vi", NarrationText = "Noi dung tieng Viet" }
                }
            },
            new Place
            {
                Id = 2, Slug = "cho-binh-tay", Category = "Market", SortOrder = 2, Status = PlaceStatus.Draft,
                Translations = new List<PlaceTranslation>()
            }
        };
        _repoMock.Setup(r => r.GetAdminListAsync(1, 10)).ReturnsAsync((places, 2));

        var result = await _service.GetListAsync(1, 10);

        Assert.Equal(2, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.True(result.Items[0].HasVietnameseNarration);
        Assert.False(result.Items[1].HasVietnameseNarration);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenPlaceNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);

        var result = await _service.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDetailDto_WhenPlaceFound()
    {
        var place = new Place { Id = 1, Slug = "dinh-binh-tien", Kind = PlaceKind.Destination, Status = PlaceStatus.Published };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("dinh-binh-tien", result!.Slug);
        Assert.Equal("Destination", result.Kind);
    }

    [Fact]
    public async Task CreateAsync_ThrowsSlugAlreadyExistsException_WhenSlugExists()
    {
        _repoMock.Setup(r => r.SlugExistsAsync("dinh-binh-tien")).ReturnsAsync(true);
        var request = new CreatePlaceRequestDto("dinh-binh-tien", "Destination", "Heritage", null, null, 10.76m, 106.63m, 1);

        await Assert.ThrowsAsync<SlugAlreadyExistsException>(() => _service.CreateAsync(request));

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Place>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AddsPlaceWithDraftStatus_WhenSlugIsUnique()
    {
        _repoMock.Setup(r => r.SlugExistsAsync("test-place")).ReturnsAsync(false);
        var request = new CreatePlaceRequestDto("test-place", "Food", "Snack", null, null, 10.76m, 106.63m, 5);

        var result = await _service.CreateAsync(request);

        Assert.Equal("test-place", result.Slug);
        Assert.Equal("Food", result.Kind);
        Assert.Equal("Draft", result.Status);
        _repoMock.Verify(r => r.AddAsync(It.Is<Place>(p => p.Slug == "test-place" && p.Status == PlaceStatus.Draft)), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenPlaceNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);
        var request = new UpdatePlaceRequestDto("Heritage", null, 10m, 106m, null, 1);

        var result = await _service.UpdateAsync(99, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndSaves_WhenPlaceFound()
    {
        var place = new Place { Id = 1, Slug = "dinh-binh-tien", Kind = PlaceKind.Destination };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);
        var request = new UpdatePlaceRequestDto("Updated Category", "Noodles", 11m, 107m, "http://img", 99);

        var result = await _service.UpdateAsync(1, request);

        Assert.NotNull(result);
        Assert.Equal("Updated Category", place.Category);
        Assert.Equal(99, place.SortOrder);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ReturnsFalse_WhenPlaceNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);

        var result = await _service.PublishAsync(99);

        Assert.False(result);
    }

    [Fact]
    public async Task PublishAsync_SetsStatusToPublished_WhenPlaceFound()
    {
        var place = new Place { Id = 1, Status = PlaceStatus.Draft };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        var result = await _service.PublishAsync(1);

        Assert.True(result);
        Assert.Equal(PlaceStatus.Published, place.Status);
    }

    [Fact]
    public async Task HideAsync_SetsStatusToHidden_WhenPlaceFound()
    {
        var place = new Place { Id = 1, Status = PlaceStatus.Published };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        var result = await _service.HideAsync(1);

        Assert.True(result);
        Assert.Equal(PlaceStatus.Hidden, place.Status);
    }
}