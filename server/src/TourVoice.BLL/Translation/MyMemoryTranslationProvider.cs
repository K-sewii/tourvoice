using System.Net;
using System.Text.Json;
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
        var url = $"get?q={HttpUtility.UrlEncode(text)}&langpair={sourceLang}|{targetLang}&de={HttpUtility.UrlEncode(_email)}";

        try
        {
            using var response = await _httpClient.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return Fail(text, TranslationChunkResult.QuotaExceededCode);
            if (!response.IsSuccessStatusCode)
                return Fail(text, $"http_{(int)response.StatusCode}");

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return Parse(doc.RootElement, text);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            return Fail(text, ex.GetType().Name);
        }
    }

    /// <summary>MyMemory tra HTTP 200 kem responseStatus rieng (so hoac chuoi); khac 200 la loi, 429 la het han muc.</summary>
    internal static TranslationChunkResult Parse(JsonElement root, string text)
    {
        var status = ReadStatus(root);
        if (status == 429) return Fail(text, TranslationChunkResult.QuotaExceededCode);
        if (status != 200) return Fail(text, $"status_{status}");

        if (!root.TryGetProperty("responseData", out var data)
            || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("translatedText", out var translated)
            || translated.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(translated.GetString()))
            return Fail(text, "empty_translation");

        decimal? match = data.TryGetProperty("match", out var m) && m.ValueKind == JsonValueKind.Number
            ? Math.Round(m.GetDecimal(), 4) : null;
        return new TranslationChunkResult(translated.GetString()!, text.Length, true, null, match);
    }

    private static int ReadStatus(JsonElement root)
    {
        if (!root.TryGetProperty("responseStatus", out var s)) return 0;
        return s.ValueKind switch
        {
            JsonValueKind.Number when s.TryGetInt32(out var n) => n,
            JsonValueKind.String when int.TryParse(s.GetString(), out var n) => n,
            _ => 0,
        };
    }

    private static TranslationChunkResult Fail(string text, string code) => new(string.Empty, text.Length, false, code);
}
