using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Configuration;

namespace TourVoice.BLL.Translation;

public class MyMemoryTranslationProvider : ITranslationProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _email;

    public MyMemoryTranslationProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _email = configuration["MyMemory:Email"]
            ?? throw new InvalidOperationException("Thieu cau hinh MyMemory:Email.");
    }

    public async Task<TranslationChunkResult> TranslateAsync(
        string text, string sourceLang, string targetLang, CancellationToken ct = default)
    {
        var query = HttpUtility.UrlEncode(text);
        var url = $"get?q={query}&langpair={sourceLang}|{targetLang}&de={_email}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<MyMemoryResponse>(url, ct);
            var translated = response?.ResponseData?.TranslatedText ?? string.Empty;
            return new TranslationChunkResult(translated, text.Length, true, null);
        }
        catch (Exception ex)
        {
            return new TranslationChunkResult(string.Empty, text.Length, false, ex.GetType().Name);
        }
    }

    private class MyMemoryResponse
    {
        public MyMemoryResponseData? ResponseData { get; set; }
    }

    private class MyMemoryResponseData
    {
        public string? TranslatedText { get; set; }
    }
}