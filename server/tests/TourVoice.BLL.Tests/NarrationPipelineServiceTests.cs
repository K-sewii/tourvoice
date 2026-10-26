using Moq;
using TourVoice.BLL.Services;
using TourVoice.BLL.Translation;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class NarrationPipelineServiceTests
{
    private readonly Mock<IPlaceRepository> _placeRepository = new();
    private readonly Mock<ILanguageRepository> _languageRepository = new();
    private readonly Mock<IUsageLogRepository> _usageLogRepository = new();
    private readonly Mock<ITranslationProvider> _translationProvider = new();
    private readonly NarrationPipelineService _service;

    public NarrationPipelineServiceTests()
    {
        _service = new NarrationPipelineService(
            _placeRepository.Object,
            _languageRepository.Object,
            _usageLogRepository.Object,
            _translationProvider.Object);
    }

    private static Place CreatePlaceWithVietnameseNarration(string narrationText = "Xin chao. Day la noi dung tieng Viet.")
    {
        var place = new Place { Id = 1, Slug = "test-place" };
        place.Translations.Add(new PlaceTranslation
        {
            PlaceId = 1,
            LanguageCode = "vi",
            Name = "Dia diem test",
            NarrationText = narrationText,
            Status = TranslationStatus.Reviewed,
            Source = TranslationSource.Human
        });
        return place;
    }

    [Fact]
    public async Task TranslatePlaceAsync_ThrowsPlaceNotFoundException_WhenPlaceDoesNotExist()
    {
        _placeRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);

        await Assert.ThrowsAsync<PlaceNotFoundException>(() => _service.TranslatePlaceAsync(99));
    }

    [Fact]
    public async Task TranslatePlaceAsync_ThrowsMissingSourceNarrationException_WhenNoVietnameseText()
    {
        var place = new Place { Id = 1, Slug = "test-place" };
        _placeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        await Assert.ThrowsAsync<MissingSourceNarrationException>(() => _service.TranslatePlaceAsync(1));
    }

    [Fact]
    public async Task TranslatePlaceAsync_ThrowsQuotaExceededException_WhenOverThreshold()
    {
        var place = CreatePlaceWithVietnameseNarration(new string('a', 1000));
        _placeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);
        _languageRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(new List<Language>
        {
            new() { Code = "vi" },
            new() { Code = "en" }
        });
        // Da dung 44.500/45.000 (nguong 90%), them 1.000 ky tu se vuot nguong
        _usageLogRepository.Setup(r => r.GetTodayCharacterTotalAsync(UsageService.Translator))
            .ReturnsAsync(44_500);

        await Assert.ThrowsAsync<QuotaExceededException>(() => _service.TranslatePlaceAsync(1));
    }

    [Fact]
    public async Task TranslatePlaceAsync_SavesDraftTranslation_WhenProviderSucceeds()
    {
        var place = CreatePlaceWithVietnameseNarration("Xin chao Quan 6.");
        _placeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);
        _languageRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(new List<Language>
        {
            new() { Code = "vi" },
            new() { Code = "en" }
        });
        _usageLogRepository.Setup(r => r.GetTodayCharacterTotalAsync(UsageService.Translator)).ReturnsAsync(0);
        _translationProvider
            .Setup(p => p.TranslateAsync(It.IsAny<string>(), "vi", "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranslationChunkResult("Hello District 6.", 16, true, null));

        var result = await _service.TranslatePlaceAsync(1);

        Assert.Single(result.Results);
        Assert.True(result.Results[0].Success);
        Assert.Equal("en", result.Results[0].LanguageCode);

        var englishTranslation = place.Translations.FirstOrDefault(t => t.LanguageCode == "en");
        Assert.NotNull(englishTranslation);
        Assert.Equal(TranslationStatus.Draft, englishTranslation!.Status);
        Assert.Equal(TranslationSource.Machine, englishTranslation.Source);

        _placeRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _usageLogRepository.Verify(r => r.AddAsync(It.IsAny<UsageLog>()), Times.Once);
    }

    [Fact]
    public async Task ApproveTranslationAsync_SetsStatusToReviewed_WhenTranslationExists()
    {
        var place = CreatePlaceWithVietnameseNarration();
        place.Translations.Add(new PlaceTranslation
        {
            PlaceId = 1,
            LanguageCode = "en",
            Name = "Test place",
            NarrationText = "Hello.",
            Status = TranslationStatus.Draft,
            Source = TranslationSource.Machine
        });
        _placeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(place);

        var ok = await _service.ApproveTranslationAsync(1, "en");

        Assert.True(ok);
        Assert.Equal(TranslationStatus.Reviewed, place.Translations.First(t => t.LanguageCode == "en").Status);
        _placeRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ApproveTranslationAsync_ReturnsFalse_WhenPlaceNotFound()
    {
        _placeRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Place?)null);

        var ok = await _service.ApproveTranslationAsync(1, "en");

        Assert.False(ok);
    }
}