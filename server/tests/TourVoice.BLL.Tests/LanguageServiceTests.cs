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
            new Language { Code = "vi", TranslatorCode = "vi", VoiceName = "vi-VN-HoaiMyNeural", IsEnabled = true },
            new Language { Code = "en", TranslatorCode = "en", VoiceName = "en-US-JennyNeural", IsEnabled = true }
        };
        _repoMock.Setup(r => r.GetEnabledAsync()).ReturnsAsync(languages);

        var result = await _service.GetLanguagesAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("vi", result[0].Code);
        Assert.Equal("vi-VN-HoaiMyNeural", result[0].VoiceName);
    }

    [Fact]
    public async Task GetLanguagesAsync_ReturnsEmptyList_WhenNoLanguagesEnabled()
    {
        _repoMock.Setup(r => r.GetEnabledAsync()).ReturnsAsync(new List<Language>());

        var result = await _service.GetLanguagesAsync();

        Assert.Empty(result);
    }
}