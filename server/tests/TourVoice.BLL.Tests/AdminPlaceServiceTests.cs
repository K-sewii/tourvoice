using Moq;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Services;
using TourVoice.BLL.Storage;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class AdminPlaceServiceTests
{
    private readonly Mock<IPlaceRepository> _repoMock = new();
    private readonly Mock<IFileStorage> _storageMock = new();
    private readonly AdminPlaceService _service;

    public AdminPlaceServiceTests()
    {
        _service = new AdminPlaceService(_repoMock.Object, _storageMock.Object);
    }

    private static Place ReadyDestination(PlaceStatus status = PlaceStatus.Draft) => new()
    {
        Id = 1, Slug = "cho-binh-tay", Kind = PlaceKind.Destination, Category = "Market",
        Latitude = 10.749m, Longitude = 106.651m, CoverImageUrl = "http://x/c.jpg", Status = status,
        Translations = { TestData.Tr("vi", "Chợ Bình Tây") },
    };

    private static CreatePlaceRequestDto Create(string slug = "new-place", string kind = "Destination",
        int? parentId = null, decimal lat = 10.749m, decimal lng = 106.651m, string? category = "Market", string? foodType = null) =>
        new(slug, kind, category, foodType, parentId, lat, lng, 1);

    // ---- tree / detail ----

    [Fact]
    public async Task GetTreeAsync_MapsChildrenAndCountsLanguagesWithText()
    {
        var parent = TestData.Destination(1, "cho-binh-tay", "Market",
            TestData.Tr("vi", "Chợ Bình Tây"), TestData.Tr("en", "Binh Tay Market", TranslationStatus.Draft));
        TestData.Food(2, "pho-bo", parent, "Noodles", TestData.Tr("vi", "Phở bò"));
        _repoMock.Setup(r => r.GetAdminTreeAsync(null, 1, 20)).ReturnsAsync((new List<Place> { parent }, 1));

        var result = await _service.GetTreeAsync(null, 1, 20);

        var node = Assert.Single(result.Items);
        Assert.Equal("Chợ Bình Tây", node.Name);
        Assert.Equal(1, node.ReviewedLanguages);
        Assert.Equal(1, node.DraftLanguages);
        Assert.True(node.HasVietnameseNarration);
        var child = Assert.Single(node.Children);
        Assert.Equal("Phở bò", child.Name);
        Assert.Equal("Food", child.Kind);
    }

    [Fact]
    public async Task GetTreeAsync_PassesStatusFilter_AndRejectsUnknownStatus()
    {
        _repoMock.Setup(r => r.GetAdminTreeAsync(PlaceStatus.Hidden, 2, 5)).ReturnsAsync((new List<Place>(), 0));

        await _service.GetTreeAsync("hidden", 2, 5);

        _repoMock.Verify(r => r.GetAdminTreeAsync(PlaceStatus.Hidden, 2, 5), Times.Once);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetTreeAsync("bogus", 1, 20));
        Assert.Equal("invalid_status", ex.Code);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenPlaceNotFound()
    {
        _repoMock.Setup(r => r.GetAdminDetailAsync(99)).ReturnsAsync((Place?)null);
        Assert.Null(await _service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByIdAsync_FlagsStaleTranslationsAndSlugLock()
    {
        var place = ReadyDestination(PlaceStatus.Published);
        place.Translations.Add(new PlaceTranslation
        {
            LanguageCode = "en", Name = "Market", NarrationText = "Hello.", Source = TranslationSource.Machine,
            SourceHash = new string('0', 64),
        });
        _repoMock.Setup(r => r.GetAdminDetailAsync(1)).ReturnsAsync(place);

        var result = await _service.GetByIdAsync(1);

        Assert.True(result!.SlugLocked);
        Assert.True(result.Translations!.Single(t => t.LanguageCode == "en").Stale);
        Assert.False(result.Translations!.Single(t => t.LanguageCode == "vi").Stale);
    }

    // ---- create (BR-02, BR-03) ----

    [Fact]
    public async Task CreateAsync_AddsDraftPlaceWithViTranslation()
    {
        var result = await _service.CreateAsync(Create() with { Name = "  Địa điểm mới " });

        Assert.Equal("Draft", result.Status);
        _repoMock.Verify(r => r.AddAsync(It.Is<Place>(p =>
            p.Status == PlaceStatus.Draft && p.Translations.Single().LanguageCode == "vi"
            && p.Translations.Single().Name == "Địa điểm mới")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenSlugExists()
    {
        _repoMock.Setup(r => r.SlugExistsAsync("new-place")).ReturnsAsync(true);
        await Assert.ThrowsAsync<SlugAlreadyExistsException>(() => _service.CreateAsync(Create()));
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Place>()), Times.Never);
    }

    [Theory]
    [InlineData("Has Space")]
    [InlineData("UPPER")]
    [InlineData("-lead")]
    [InlineData("")]
    public async Task CreateAsync_RejectsInvalidSlug(string slug)
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(Create(slug)));
        Assert.Equal("invalid_slug", ex.Code);
    }

    [Theory]
    [InlineData(10.70, 106.65)]   // vi do thap hon vung phuc vu
    [InlineData(10.75, 106.70)]   // kinh do vuot vung
    public async Task CreateAsync_RejectsCoordinatesOutsideServiceArea_BR03(double lat, double lng)
    {
        await Assert.ThrowsAsync<OutsideServiceAreaException>(
            () => _service.CreateAsync(Create(lat: (decimal)lat, lng: (decimal)lng)));
    }

    [Fact]
    public async Task CreateAsync_FoodNeedsDestinationParent_BR02()
    {
        var food = new Place { Id = 5, Kind = PlaceKind.Food };
        _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(food);
        _repoMock.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Place?)null);

        foreach (var parentId in new int?[] { null, 404, 5 })
        {
            var ex = await Assert.ThrowsAsync<BusinessRuleException>(
                () => _service.CreateAsync(Create(kind: "Food", parentId: parentId, category: null, foodType: "Noodles")));
            Assert.Equal("invalid_parent", ex.Code);
        }
    }

    [Fact]
    public async Task CreateAsync_FoodUnderDestination_KeepsOnlyFoodType()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ReadyDestination());

        var result = await _service.CreateAsync(Create(kind: "food", parentId: 1, category: "Market", foodType: "Noodles"));

        Assert.Equal(1, result.ParentId);
        Assert.Equal("Noodles", result.FoodType);
        Assert.Null(result.Category);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownKindCategoryAndFoodType()
    {
        Assert.Equal("invalid_kind",
            (await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(Create(kind: "Cafe")))).Code);
        Assert.Equal("invalid_category",
            (await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(Create(category: "Mall")))).Code);

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ReadyDestination());
        Assert.Equal("invalid_food_type",
            (await Assert.ThrowsAsync<ValidationException>(
                () => _service.CreateAsync(Create(kind: "Food", parentId: 1, category: null, foodType: "Pizza")))).Code);
    }

    // ---- update (slug lock, BR-03) ----

    private static UpdatePlaceRequestDto Update(string? slug = null, decimal lat = 10.749m, decimal lng = 106.651m) =>
        new("Market", null, lat, lng, "http://x/c.jpg", 3, Slug: slug);

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);
        Assert.Null(await _service.UpdateAsync(99, Update()));
    }

    [Fact]
    public async Task UpdateAsync_SavesFields()
    {
        var place = ReadyDestination();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        await _service.UpdateAsync(1, Update());

        Assert.Equal(3, place.SortOrder);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SavesGeofenceRadius_AndKeepsItWhenOmitted()
    {
        var place = ReadyDestination();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        var dto = await _service.UpdateAsync(1, Update() with { GeofenceRadiusM = 250 });
        Assert.Equal(250, place.GeofenceRadiusM);
        Assert.Equal(250, dto!.GeofenceRadiusM);

        await _service.UpdateAsync(1, Update());   // khong gui -> giu nguyen
        Assert.Equal(250, place.GeofenceRadiusM);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, Update() with { GeofenceRadiusM = 5 }));
    }

    [Fact]
    public async Task UpdateAsync_ChangesSlug_OnlyWhileDraft()
    {
        var draft = ReadyDestination();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(draft);
        await _service.UpdateAsync(1, Update("moi-slug"));
        Assert.Equal("moi-slug", draft.Slug);

        var published = ReadyDestination(PlaceStatus.Published);
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(published);
        await Assert.ThrowsAsync<SlugLockedException>(() => _service.UpdateAsync(2, Update("khac")));
        Assert.Equal("cho-binh-tay", published.Slug);
    }

    [Fact]
    public async Task UpdateAsync_AllowsSameSlugOnPublishedPlace_AndRejectsTakenSlug()
    {
        var published = ReadyDestination(PlaceStatus.Published);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(published);
        await _service.UpdateAsync(1, Update("cho-binh-tay"));   // khong doi -> khong bi khoa chan

        var draft = ReadyDestination();
        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(draft);
        _repoMock.Setup(r => r.SlugExistsAsync("da-co")).ReturnsAsync(true);
        await Assert.ThrowsAsync<SlugAlreadyExistsException>(() => _service.UpdateAsync(2, Update("da-co")));
    }

    [Fact]
    public async Task UpdateAsync_RejectsOutsideServiceArea()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ReadyDestination());
        await Assert.ThrowsAsync<OutsideServiceAreaException>(() => _service.UpdateAsync(1, Update(lat: 11m)));
    }

    // ---- publish / hide (BR-08) ----

    [Fact]
    public async Task PublishAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);
        Assert.False(await _service.PublishAsync(99));
    }

    [Fact]
    public async Task PublishAsync_Publishes_WhenAllPreconditionsMet()
    {
        var place = ReadyDestination();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        Assert.True(await _service.PublishAsync(1));

        Assert.Equal(PlaceStatus.Published, place.Status);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ListsEveryMissingItem_BR08()
    {
        var place = new Place { Id = 1, Slug = "x", Kind = PlaceKind.Destination, Latitude = 0, Longitude = 0 };
        place.Translations.Add(TestData.Tr("vi", "X", TranslationStatus.Draft));
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        var ex = await Assert.ThrowsAsync<PublishPreconditionsException>(() => _service.PublishAsync(1));

        Assert.Equal(["vi_narration_reviewed", "coordinates", "cover_image", "category"], ex.Missing);
        Assert.Equal(PlaceStatus.Draft, place.Status);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_RequiresReviewedViWithText()
    {
        var place = ReadyDestination();
        place.Translations.Clear();
        place.Translations.Add(new PlaceTranslation { LanguageCode = "vi", Name = "X", Status = TranslationStatus.Reviewed, NarrationText = " " });
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        var ex = await Assert.ThrowsAsync<PublishPreconditionsException>(() => _service.PublishAsync(1));
        Assert.Equal(["vi_narration_reviewed"], ex.Missing);
    }

    [Fact]
    public async Task HideAsync_SetsHidden_OrReturnsFalse()
    {
        var place = ReadyDestination(PlaceStatus.Published);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);

        Assert.True(await _service.HideAsync(1));
        Assert.Equal(PlaceStatus.Hidden, place.Status);
        Assert.False(await _service.HideAsync(99));
    }

    // ---- delete (BR-13) ----

    [Fact]
    public async Task DeleteAsync_RemovesDraftWithoutChildren()
    {
        var place = ReadyDestination();
        place.CoverImageUrl = "http://api/uploads/covers/a.png";
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        Assert.True(await _service.DeleteAsync(1));

        _repoMock.Verify(r => r.RemoveAsync(place), Times.Once);
        _storageMock.Verify(s => s.DeleteAsync("http://api/uploads/covers/a.png"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Refuses_NonDraftOrWithChildren_BR13()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ReadyDestination(PlaceStatus.Published));
        var notDraft = await Assert.ThrowsAsync<DeleteNotAllowedException>(() => _service.DeleteAsync(1));
        Assert.Equal("place_not_draft", notDraft.Code);

        _repoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(ReadyDestination());
        _repoMock.Setup(r => r.HasChildrenAsync(2)).ReturnsAsync(true);
        var hasChildren = await Assert.ThrowsAsync<DeleteNotAllowedException>(() => _service.DeleteAsync(2));
        Assert.Equal("place_has_children", hasChildren.Code);

        _repoMock.Verify(r => r.RemoveAsync(It.IsAny<Place>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);
        Assert.False(await _service.DeleteAsync(99));
    }

    // ---- cover upload ----

    private static byte[] Png(int extra = 20) =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[extra]];

    [Fact]
    public async Task SetCoverAsync_StoresValidImage_AndDeletesPreviousCover()
    {
        var place = ReadyDestination();
        place.CoverImageUrl = "http://api/uploads/covers/old.png";
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);
        byte[]? saved = null;
        _storageMock.Setup(s => s.SaveAsync(It.IsAny<Stream>(), "covers", ".png", It.IsAny<CancellationToken>()))
            .Callback<Stream, string, string, CancellationToken>((s, _, _, _) => saved = ((MemoryStream)s).ToArray())
            .ReturnsAsync("http://api/uploads/covers/new.png");
        var bytes = Png();

        var result = await _service.SetCoverAsync(1, new MemoryStream(bytes), bytes.Length);

        Assert.Equal("http://api/uploads/covers/new.png", result!.CoverImageUrl);
        Assert.Equal(bytes, saved);   // header da doc thu van duoc ghi lai day du
        _storageMock.Verify(s => s.DeleteAsync("http://api/uploads/covers/old.png"), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetCoverAsync_Rejects_NonImage_Empty_AndOversize()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ReadyDestination());
        var text = "<html>not an image</html>"u8.ToArray();

        Assert.Equal("invalid_image",
            (await Assert.ThrowsAsync<ValidationException>(() => _service.SetCoverAsync(1, new MemoryStream(text), text.Length))).Code);
        Assert.Equal("empty_file",
            (await Assert.ThrowsAsync<ValidationException>(() => _service.SetCoverAsync(1, new MemoryStream(), 0))).Code);
        Assert.Equal("file_too_large",
            (await Assert.ThrowsAsync<ValidationException>(
                () => _service.SetCoverAsync(1, new MemoryStream(Png()), AdminPlaceService.MaxCoverBytes + 1))).Code);

        _storageMock.Verify(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetCoverAsync_ReturnsNull_WhenPlaceNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);
        Assert.Null(await _service.SetCoverAsync(99, new MemoryStream(Png()), 28));
    }

    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0 }, ".jpg")]
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 1, 2, 3, 4, 0x57, 0x45, 0x42, 0x50 }, ".webp")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0, 0, 0, 0, 0, 0 }, null)]   // GIF khong duoc nhan
    public void DetectImageExtension_ByMagicBytes(byte[] header, string? expected) =>
        Assert.Equal(expected, AdminPlaceService.DetectImageExtension(header));
}
