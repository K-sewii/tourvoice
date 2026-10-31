using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using TourVoice.BLL.Translation;
using Xunit;

namespace TourVoice.BLL.Tests;

public class MyMemoryProviderTests
{
    private static TranslationChunkResult Parse(string json, string text = "Xin chào.") =>
        MyMemoryTranslationProvider.Parse(JsonDocument.Parse(json).RootElement, text);

    [Fact]
    public void Parse_Status200_ReturnsTextAndMatch()
    {
        var r = Parse("""{"responseData":{"translatedText":"Hello.","match":0.98766},"responseStatus":200}""");

        Assert.True(r.Success);
        Assert.Equal("Hello.", r.TranslatedText);
        Assert.Equal(0.9877m, r.Match);
        Assert.Equal("Xin chào.".Length, r.CharactersUsed);
    }

    [Fact]
    public void Parse_AcceptsStatusAsString_AndMissingMatch()
    {
        var r = Parse("""{"responseData":{"translatedText":"Hello."},"responseStatus":"200"}""");

        Assert.True(r.Success);
        Assert.Null(r.Match);
    }

    [Theory]
    [InlineData("""{"responseData":{"translatedText":"MYMEMORY WARNING: YOU USED ALL AVAILABLE FREE TRANSLATIONS"},"responseStatus":403}""", "status_403")]
    [InlineData("""{"responseData":{"translatedText":"QUERY LENGTH LIMIT EXCEEDED"},"responseStatus":"414"}""", "status_414")]
    [InlineData("""{"responseData":{"translatedText":"x"}}""", "status_0")]
    public void Parse_NonOkStatus_IsFailure_AndNeverReturnsErrorTextAsTranslation(string json, string code)
    {
        var r = Parse(json);

        Assert.False(r.Success);
        Assert.Equal(code, r.ErrorCode);
        Assert.Equal("", r.TranslatedText);   // loi cu: van ban loi cua MyMemory bi luu thanh ban dich
    }

    [Fact]
    public void Parse_Status429_MapsToQuotaCode()
    {
        var r = Parse("""{"responseData":{"translatedText":"WARNING"},"responseStatus":429}""");
        Assert.Equal(TranslationChunkResult.QuotaExceededCode, r.ErrorCode);
    }

    [Theory]
    [InlineData("""{"responseStatus":200}""")]
    [InlineData("""{"responseData":null,"responseStatus":200}""")]
    [InlineData("""{"responseData":{"translatedText":"  "},"responseStatus":200}""")]
    public void Parse_Status200ButNoText_IsFailure(string json)
    {
        var r = Parse(json);
        Assert.False(r.Success);
        Assert.Equal("empty_translation", r.ErrorCode);
    }

    // ---- HTTP level ----

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            return Task.FromResult(respond(request));
        }
    }

    private static MyMemoryTranslationProvider MakeProvider(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.mymemory.translated.net/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["MyMemory:Email"] = "team@x.vn" }).Build();
        return new MyMemoryTranslationProvider(http, config);
    }

    private static (MyMemoryTranslationProvider Provider, StubHandler Handler) Make(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        return (MakeProvider(handler), handler);
    }

    [Fact]
    public async Task Translate_SendsLangPairAndContactEmail()
    {
        var (provider, handler) = Make(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"responseData":{"translatedText":"Hello."},"responseStatus":200}"""),
        });

        var r = await provider.TranslateAsync("Xin chào.", "vi", "zh-CN");

        Assert.True(r.Success);
        var query = handler.Last!.RequestUri!.Query;
        Assert.Contains("langpair=vi|zh-CN", Uri.UnescapeDataString(query));
        Assert.Contains("de=team%40x.vn", query);
    }

    [Fact]
    public async Task Translate_Http429_IsQuota_OtherHttpErrorsAreFailures()
    {
        var (quota, _) = Make(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var (broken, _) = Make(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));

        Assert.Equal(TranslationChunkResult.QuotaExceededCode, (await quota.TranslateAsync("x", "vi", "en")).ErrorCode);
        Assert.Equal("http_502", (await broken.TranslateAsync("x", "vi", "en")).ErrorCode);
    }

    [Fact]
    public async Task Translate_NetworkOrBodyErrors_BecomeFailureResults()
    {
        var (offline, _) = Make(_ => throw new HttpRequestException("down"));
        var (garbage, _) = Make(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>") });

        Assert.False((await offline.TranslateAsync("x", "vi", "en")).Success);
        Assert.False((await garbage.TranslateAsync("x", "vi", "en")).Success);
    }

    [Fact]
    public async Task Translate_RetriesOnce_WhenServerFailsTransiently()
    {
        var calls = 0;
        var (provider, _) = Make(_ => ++calls == 1
            ? new HttpResponseMessage(HttpStatusCode.BadGateway)
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"responseData":{"translatedText":"Hello."},"responseStatus":200}"""),
            });

        var r = await provider.TranslateAsync("Xin chào.", "vi", "en");

        Assert.True(r.Success);
        Assert.Equal("Hello.", r.TranslatedText);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Translate_DoesNotRetry_WhenQuotaIsExhausted()
    {
        var calls = 0;
        var (provider, _) = Make(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        });

        var r = await provider.TranslateAsync("x", "vi", "en");

        Assert.Equal(TranslationChunkResult.QuotaExceededCode, r.ErrorCode);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Translate_CallerCancellation_PropagatesInsteadOfBecomingAFailure()
    {
        var provider = MakeProvider(new HangingHandler());
        using var cts = new CancellationTokenSource(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.TranslateAsync("x", "vi", "en", cts.Token));
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
