using Moq;
using TourVoice.BLL.Services;
using TourVoice.DAL.Entities;
using TourVoice.DAL.Repositories;
using Xunit;

namespace TourVoice.BLL.Tests;

public class LanguageServiceTests
{
    private readonly Mock<ILanguageRepository> _repoMock = new();
    private readonly LanguageService _service;

    public LanguageServiceTests()
    {
        _service = new LanguageService(_repoMock.Object);
    }

    [Fact]
    public async Task GetLanguagesAsync_ReturnsMappedList_WhenLanguagesExist()
    {
        var languages = new List<Language>
        {
            new Language { Code = "vi", MtCode = "vi", SpeechLang = "vi-VN", IsEnabled = true },
            new Language { Code = "zh", MtCode = "zh-CN", SpeechLang = "zh-CN", IsEnabled = true }
        };
        _repoMock.Setup(r => r.GetEnabledAsync()).ReturnsAsync(languages);

        var result = await _service.GetLanguagesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("vi", result[0].Code);
        Assert.Equal("vi-VN", result[0].SpeechLang);
        Assert.Equal("zh-CN", result[1].MtCode);
    }

    [Fact]
    public async Task GetAllAsync_IncludesDisabledLanguages()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Language>
        {
            new() { Code = "vi", MtCode = "vi", SpeechLang = "vi-VN", IsEnabled = true },
            new() { Code = "ko", MtCode = "ko", SpeechLang = "ko-KR", IsEnabled = false },
        });

        var result = await _service.GetAllAsync();

        Assert.False(result.Single(l => l.Code == "ko").IsEnabled);
    }

    [Fact]
    public async Task UpdateAsync_SavesTrimmedCodes()
    {
        var zh = new Language { Code = "zh", MtCode = "zh-CN", SpeechLang = "zh-CN", IsEnabled = true };
        _repoMock.Setup(r => r.GetByCodeAsync("zh")).ReturnsAsync(zh);

        var dto = await _service.UpdateAsync("zh", new(" zh-TW ", " zh-TW ", false));

        Assert.Equal("zh-TW", zh.MtCode);
        Assert.Equal("zh-TW", zh.SpeechLang);
        Assert.False(dto!.IsEnabled);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenLanguageMissing()
    {
        _repoMock.Setup(r => r.GetByCodeAsync("xx")).ReturnsAsync((Language?)null);
        Assert.Null(await _service.UpdateAsync("xx", new("xx", "xx-XX", true)));
    }

    [Theory]
    [InlineData("", "en-US", "invalid_mt_code")]
    [InlineData("english", "en-US", "invalid_mt_code")]
    [InlineData("en", "english", "invalid_speech_lang")]
    [InlineData("en", "en", "invalid_speech_lang")]
    public async Task UpdateAsync_RejectsMalformedCodes(string mt, string speech, string code)
    {
        _repoMock.Setup(r => r.GetByCodeAsync("en")).ReturnsAsync(new Language { Code = "en", MtCode = "en", SpeechLang = "en-US" });

        var ex = await Assert.ThrowsAsync<TourVoice.BLL.Exceptions.ValidationException>(
            () => _service.UpdateAsync("en", new(mt, speech, true)));

        Assert.Equal(code, ex.Code);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CannotDisableVietnamese_TheSourceLanguage()
    {
        _repoMock.Setup(r => r.GetByCodeAsync("vi")).ReturnsAsync(new Language { Code = "vi", MtCode = "vi", SpeechLang = "vi-VN" });

        var ex = await Assert.ThrowsAsync<TourVoice.BLL.Exceptions.BusinessRuleException>(
            () => _service.UpdateAsync("vi", new("vi", "vi-VN", false)));

        Assert.Equal("source_language_required", ex.Code);
    }

    [Fact]
    public async Task GetLanguagesAsync_ReturnsEmptyList_WhenNoLanguagesEnabled()
    {
        _repoMock.Setup(r => r.GetEnabledAsync()).ReturnsAsync(new List<Language>());

        var result = await _service.GetLanguagesAsync();

        Assert.Empty(result);
    }
}