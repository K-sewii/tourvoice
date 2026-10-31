using System.Text;
using Moq;
using TourVoice.BLL.Exceptions;
using TourVoice.BLL.Services;
using TourVoice.BLL.Translation;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class NarrationPipelineServiceTests
{
    private const string ViText = "Xin chào Quận 6.";

    private readonly Mock<IPlaceRepository> _placeRepository = new();
    private readonly Mock<ILanguageRepository> _languageRepository = new();
    private readonly Mock<IUsageLogRepository> _usageLogRepository = new();
    private readonly Mock<ITranslationProvider> _translationProvider = new();
    private readonly NarrationPipelineService _service;

    public NarrationPipelineServiceTests()
    {
        _service = new NarrationPipelineService(
            _placeRepository.Object, _languageRepository.Object,
            _usageLogRepository.Object, _translationProvider.Object, TimeProvider.System);
        _languageRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(Langs("vi", "en"));
        _languageRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(Langs("vi", "en"));
    }

    private static List<Language> Langs(params string[] codes) => codes.Select(c => new Language
    {
        Code = c, MtCode = c == "zh" ? "zh-CN" : c, SpeechLang = c, IsEnabled = true,
    }).ToList();

    private static Place PlaceWithVi(string text = ViText, TranslationStatus status = TranslationStatus.Reviewed)
    {
        var place = new Place { Id = 1, Slug = "test-place" };
        place.Translations.Add(new PlaceTranslation
        {
            PlaceId = 1, LanguageCode = "vi", Name = "Địa điểm", NarrationText = text,
            Status = status, Source = TranslationSource.Manual,
        });
        return place;
    }

    private Place Setup(Place place)
    {
        _placeRepository.Setup(r => r.GetByIdAsync(place.Id)).ReturnsAsync(place);
        return place;
    }

    private void ProviderReturns(string lang, string text, decimal? match = 0.9m) =>
        _translationProvider.Setup(p => p.TranslateAsync(It.IsAny<string>(), "vi", lang, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranslationChunkResult(text, 16, true, null, match));

    // ---- translate: preconditions ----

    [Fact]
    public async Task Translate_Throws_WhenPlaceMissing()
    {
        _placeRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);
        await Assert.ThrowsAsync<PlaceNotFoundException>(() => _service.TranslatePlaceAsync(99));
    }

    [Fact]
    public async Task Translate_Throws_WhenNoVietnamese()
    {
        Setup(new Place { Id = 1, Slug = "p" });
        var ex = await Assert.ThrowsAsync<MissingSourceNarrationException>(() => _service.TranslatePlaceAsync(1));
        Assert.Equal("source_not_reviewed", ex.Code);
    }

    [Fact]
    public async Task Translate_Throws_WhenVietnameseNotReviewed_BR04()
    {
        Setup(PlaceWithVi(status: TranslationStatus.Draft));
        await Assert.ThrowsAsync<MissingSourceNarrationException>(() => _service.TranslatePlaceAsync(1));
        _translationProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Translate_Throws429_WhenEstimateCrossesNinetyPercent_BR06()
    {
        Setup(PlaceWithVi(new string('a', 1000)));
        _usageLogRepository.Setup(r => r.GetCharacterTotalSinceAsync(UsageService.MyMemory, It.IsAny<DateTime>())).ReturnsAsync(44_500);

        await Assert.ThrowsAsync<QuotaExceededException>(() => _service.TranslatePlaceAsync(1));
        _translationProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Translate_AllowsExactlyTheThreshold()
    {
        Setup(PlaceWithVi(new string('a', 1000)));
        _usageLogRepository.Setup(r => r.GetCharacterTotalSinceAsync(UsageService.MyMemory, It.IsAny<DateTime>())).ReturnsAsync(44_000);
        ProviderReturns("en", "ok");

        var result = await _service.TranslatePlaceAsync(1);   // 44.000 + 1.000 = 45.000, khong vuot

        Assert.True(result.Results.Single().Success);
    }

    // ---- translate: success, BR-05, failure ----

    [Fact]
    public async Task Translate_SavesMachineDraft_WithHashAndMatch_InOneTransaction()
    {
        var place = Setup(PlaceWithVi());
        ProviderReturns("en", "Hello District 6.", 0.87m);

        var result = await _service.TranslatePlaceAsync(1);

        var en = place.Translations.Single(t => t.LanguageCode == "en");
        Assert.Equal(TranslationStatus.Draft, en.Status);
        Assert.Equal(TranslationSource.Machine, en.Source);
        Assert.Equal(TranslationRules.HashOf(ViText), en.SourceHash);
        Assert.Equal(0.87m, en.MachineMatch);
        Assert.False(result.Results.Single().Skipped);
        _usageLogRepository.Verify(r => r.Add(It.Is<UsageLog>(l => l.Success && l.Chunks == 1)), Times.Once);
        _placeRepository.Verify(r => r.SaveChangesAsync(), Times.Once);   // mot SaveChanges = mot transaction
    }

    [Fact]
    public async Task Translate_SkipsLanguageWhenHashUnchanged_BR05()
    {
        var place = PlaceWithVi();
        place.Translations.Add(new PlaceTranslation
        {
            PlaceId = 1, LanguageCode = "en", Name = "x", NarrationText = "Hello.",
            Status = TranslationStatus.Reviewed, Source = TranslationSource.Machine,
            SourceHash = TranslationRules.HashOf(ViText),
        });
        Setup(place);

        var result = await _service.TranslatePlaceAsync(1);

        Assert.True(result.Results.Single().Skipped);
        _translationProvider.VerifyNoOtherCalls();
        _usageLogRepository.Verify(r => r.Add(It.IsAny<UsageLog>()), Times.Never);
        Assert.Equal(TranslationStatus.Reviewed, place.Translations.Single(t => t.LanguageCode == "en").Status);
    }

    [Fact]
    public async Task Translate_RetranslatesStaleLanguage_AndDropsItBackToDraft_BR05()
    {
        var place = PlaceWithVi("Bài đã sửa.");
        place.Translations.Add(new PlaceTranslation
        {
            PlaceId = 1, LanguageCode = "en", Name = "x", NarrationText = "Old.",
            Status = TranslationStatus.Reviewed, Source = TranslationSource.Machine,
            SourceHash = TranslationRules.HashOf("Bài cũ."),
        });
        Setup(place);
        ProviderReturns("en", "Edited.");

        await _service.TranslatePlaceAsync(1);

        var en = place.Translations.Single(t => t.LanguageCode == "en");
        Assert.Equal("Edited.", en.NarrationText);
        Assert.Equal(TranslationStatus.Draft, en.Status);
        Assert.Equal(TranslationRules.HashOf("Bài đã sửa."), en.SourceHash);
    }

    [Fact]
    public async Task Translate_NeverOverwritesManualTranslationWithoutHash()
    {
        var place = PlaceWithVi();
        place.Translations.Add(new PlaceTranslation
        {
            PlaceId = 1, LanguageCode = "en", Name = "x", NarrationText = "Hand written.",
            Status = TranslationStatus.Reviewed, Source = TranslationSource.Manual,
        });
        Setup(place);

        var result = await _service.TranslatePlaceAsync(1);

        Assert.True(result.Results.Single().Skipped);
        Assert.Equal("Hand written.", place.Translations.Single(t => t.LanguageCode == "en").NarrationText);
    }

    [Fact]
    public async Task Translate_On502_KeepsEarlierLanguages_LogsFailure_AndStops()
    {
        _languageRepository.Setup(r => r.GetEnabledAsync()).ReturnsAsync(Langs("vi", "en", "zh", "ko"));
        var place = Setup(PlaceWithVi());
        ProviderReturns("en", "Hello.");
        _translationProvider.Setup(p => p.TranslateAsync(It.IsAny<string>(), "vi", "zh-CN", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranslationChunkResult("", 16, false, "status_403"));

        var ex = await Assert.ThrowsAsync<ExternalServiceException>(() => _service.TranslatePlaceAsync(1));

        Assert.Contains("status_403", ex.Message);
        Assert.Equal(TranslationStatus.Draft, place.Translations.Single(t => t.LanguageCode == "en").Status);
        Assert.DoesNotContain(place.Translations, t => t.LanguageCode == "zh");
        _translationProvider.Verify(p => p.TranslateAsync(It.IsAny<string>(), "vi", "ko", It.IsAny<CancellationToken>()), Times.Never);
        _usageLogRepository.Verify(r => r.Add(It.Is<UsageLog>(l => !l.Success && l.ErrorCode == "status_403")), Times.Once);
        _placeRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Translate_MapsProviderQuotaCodeTo429()
    {
        Setup(PlaceWithVi());
        _translationProvider.Setup(p => p.TranslateAsync(It.IsAny<string>(), "vi", "en", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TranslationChunkResult("", 16, false, TranslationChunkResult.QuotaExceededCode));

        await Assert.ThrowsAsync<QuotaExceededException>(() => _service.TranslatePlaceAsync(1));
        _placeRepository.Verify(r => r.SaveChangesAsync(), Times.Once);   // dong UsageLogs van duoc ghi
    }

    [Fact]
    public async Task Translate_ClipsOverlongResultToFourThousandChars()
    {
        var place = Setup(PlaceWithVi());
        ProviderReturns("en", string.Join(" ", Enumerable.Repeat("Sentence number one.", 300)));

        await _service.TranslatePlaceAsync(1);

        var text = place.Translations.Single(t => t.LanguageCode == "en").NarrationText!;
        Assert.InRange(text.Length, 1, 4000);
        Assert.EndsWith(".", text);
    }

    // ---- chunking ----

    [Theory]
    [InlineData("Câu ngắn. Câu thứ hai! Câu thứ ba?")]
    [InlineData("Một câu rất dài không có dấu chấm nào cả mà vẫn phải được cắt theo từ để vừa giới hạn byte của MyMemory")]
    public void ChunkText_EveryChunkFitsFiveHundredBytes(string seed)
    {
        var text = string.Join(" ", Enumerable.Repeat(seed, 40));

        var chunks = NarrationPipelineService.ChunkText(text);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.InRange(Encoding.UTF8.GetByteCount(c), 1, 500));
    }

    [Fact]
    public void ChunkText_KeepsShortTextWhole_AndLosesNoWords()
    {
        Assert.Equal(["Một câu. Hai câu."], NarrationPipelineService.ChunkText("Một câu. Hai câu."));

        var text = string.Join(" ", Enumerable.Range(1, 200).Select(i => $"Từ{i}."));
        var rejoined = string.Join(" ", NarrationPipelineService.ChunkText(text));
        Assert.Equal(text, rejoined);
    }

    [Fact]
    public void ChunkText_SplitsCjkWithoutSpaces_OnRuneBoundaries()
    {
        var text = new string('越', 1000);   // 3 byte moi ky tu, khong dau cach

        var chunks = NarrationPipelineService.ChunkText(text);

        Assert.All(chunks, c => Assert.True(Encoding.UTF8.GetByteCount(c) <= 500));
        Assert.Equal(text, string.Concat(chunks));
    }

    // ---- approve / update (BR-07) ----

    [Fact]
    public async Task Approve_SetsReviewed()
    {
        var place = PlaceWithVi(status: TranslationStatus.Draft);
        Setup(place);

        Assert.True(await _service.ApproveTranslationAsync(1, "vi"));

        Assert.Equal(TranslationStatus.Reviewed, place.Translations.Single().Status);
        _placeRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Approve_ReturnsFalse_ForMissingPlaceOrTranslation()
    {
        _placeRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);
        Setup(PlaceWithVi());

        Assert.False(await _service.ApproveTranslationAsync(99, "vi"));
        Assert.False(await _service.ApproveTranslationAsync(1, "ko"));
    }

    [Fact]
    public async Task Approve_Refuses_EmptyNarration()
    {
        var place = PlaceWithVi("  ", TranslationStatus.Draft);
        Setup(place);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.ApproveTranslationAsync(1, "vi"));

        Assert.Equal("empty_narration", ex.Code);
        Assert.Equal(TranslationStatus.Draft, place.Translations.Single().Status);
    }

    [Fact]
    public async Task UpdateTranslation_OnReviewed_DropsToDraftAndMarksManual_BR07()
    {
        var place = PlaceWithVi();
        place.Translations.Single().Source = TranslationSource.Machine;
        place.Translations.Single().MachineMatch = 0.9m;
        Setup(place);
        _languageRepository.Setup(r => r.GetByCodeAsync("vi")).ReturnsAsync(Langs("vi")[0]);

        var dto = await _service.UpdateTranslationAsync(1, "vi", new(" Tên mới ", "Nội dung mới."));

        var vi = place.Translations.Single();
        Assert.Equal("Draft", dto!.Status);
        Assert.Equal(TranslationStatus.Draft, vi.Status);
        Assert.Equal(TranslationSource.Manual, vi.Source);
        Assert.Null(vi.MachineMatch);
        Assert.Equal("Tên mới", vi.Name);
        _placeRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateTranslation_WithNoChange_KeepsReviewed()
    {
        var place = PlaceWithVi();
        Setup(place);
        _languageRepository.Setup(r => r.GetByCodeAsync("vi")).ReturnsAsync(Langs("vi")[0]);

        await _service.UpdateTranslationAsync(1, "vi", new("Địa điểm", ViText));

        Assert.Equal(TranslationStatus.Reviewed, place.Translations.Single().Status);
    }

    [Fact]
    public async Task UpdateTranslation_CreatesMissingLanguageAsManualDraft()
    {
        var place = Setup(PlaceWithVi());
        _languageRepository.Setup(r => r.GetByCodeAsync("en")).ReturnsAsync(Langs("en")[0]);

        var dto = await _service.UpdateTranslationAsync(1, "en", new("Place", "Hello."));

        Assert.Equal("Draft", dto!.Status);
        Assert.Equal("Manual", dto.Source);
        Assert.Contains(place.Translations, t => t.LanguageCode == "en");
    }

    [Fact]
    public async Task UpdateTranslation_Validates()
    {
        Setup(PlaceWithVi());
        _languageRepository.Setup(r => r.GetByCodeAsync("vi")).ReturnsAsync(Langs("vi")[0]);
        _languageRepository.Setup(r => r.GetByCodeAsync("xx")).ReturnsAsync((Language?)null);
        _placeRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Place?)null);

        Assert.Null(await _service.UpdateTranslationAsync(99, "vi", new("A", "b")));
        await Assert.ThrowsAsync<LanguageNotFoundException>(() => _service.UpdateTranslationAsync(1, "xx", new("A", "b")));
        Assert.Equal("invalid_name",
            (await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateTranslationAsync(1, "vi", new("  ", "b")))).Code);
        Assert.Equal("narration_too_long",
            (await Assert.ThrowsAsync<ValidationException>(
                () => _service.UpdateTranslationAsync(1, "vi", new("A", new string('a', 4001))))).Code);
    }

    // ---- matrix (SCR-A5) ----

    [Fact]
    public async Task Matrix_ReportsCellStatusStaleAndCanTranslate()
    {
        var a = PlaceWithVi();   // vi Reviewed, thieu en -> dich duoc
        var b = new Place { Id = 2, Slug = "b" };
        b.Translations.Add(new PlaceTranslation { PlaceId = 2, LanguageCode = "vi", Name = "B", NarrationText = "Bài B.", Status = TranslationStatus.Reviewed });
        b.Translations.Add(new PlaceTranslation
        {
            PlaceId = 2, LanguageCode = "en", Name = "B", NarrationText = "B text.", Status = TranslationStatus.Draft,
            Source = TranslationSource.Machine, SourceHash = new string('0', 64),
        });
        _placeRepository.Setup(r => r.GetAllWithTranslationsAsync()).ReturnsAsync([a, b]);

        var matrix = await _service.GetMatrixAsync(null);

        Assert.Equal(["vi", "en"], matrix.Languages.Select(l => l.Code));
        Assert.Equal("None", matrix.Rows[0].Cells["en"].Status);
        Assert.True(matrix.Rows[0].CanTranslate);
        Assert.Equal("Draft", matrix.Rows[1].Cells["en"].Status);
        Assert.True(matrix.Rows[1].Cells["en"].Stale);
        Assert.True(matrix.Rows[1].CanTranslate);   // ban goc da doi -> dich lai duoc
    }

    [Theory]
    [InlineData("draft", new[] { 2 })]
    [InlineData("stale", new[] { 2 })]
    [InlineData("missing", new[] { 1 })]
    [InlineData("all", new[] { 1, 2 })]
    public async Task Matrix_FiltersRows(string filter, int[] expectedIds)
    {
        var a = PlaceWithVi();
        var b = new Place { Id = 2, Slug = "b" };
        b.Translations.Add(new PlaceTranslation { PlaceId = 2, LanguageCode = "vi", Name = "B", NarrationText = "Bài B.", Status = TranslationStatus.Reviewed });
        b.Translations.Add(new PlaceTranslation
        {
            PlaceId = 2, LanguageCode = "en", Name = "B", NarrationText = "B text.", Status = TranslationStatus.Draft,
            Source = TranslationSource.Machine, SourceHash = new string('0', 64),
        });
        _placeRepository.Setup(r => r.GetAllWithTranslationsAsync()).ReturnsAsync([a, b]);

        var matrix = await _service.GetMatrixAsync(filter);

        Assert.Equal(expectedIds, matrix.Rows.Select(r => r.PlaceId));
    }

    [Fact]
    public async Task Matrix_RejectsUnknownFilter()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.GetMatrixAsync("weird"));
        Assert.Equal("invalid_filter", ex.Code);
    }
}
