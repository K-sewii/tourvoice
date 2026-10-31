using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TourVoice.BLL.Auth;

namespace TourVoice.API.Tests;

/// <summary>
/// Kiem thu tang Presentation qua HTTP that (khong can MySQL): ProblemDetails, phan quyen,
/// presence va gioi han toc do. Cac duong nay khong cham CSDL.
/// </summary>
public class ApiTests : IClassFixture<ApiTests.Factory>
{
    private const string JwtKey = "api-test-secret-key-it-nhat-32-byte-hs256!!";
    private readonly Factory _factory;

    public ApiTests(Factory factory) => _factory = factory;

    public sealed class Factory : WebApplicationFactory<Program>
    {
        /// <summary>Dat true de gia lap tai khoan trong token da bi khoa.</summary>
        public bool Locked { get; init; }

        public string UploadsDir { get; } = Path.Combine(Path.GetTempPath(), "tourvoice-api-tests", Guid.NewGuid().ToString("N"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Uploads:Path", UploadsDir);   // khong tao thu muc uploads trong cay ma nguon
            builder.UseEnvironment("Testing");   // khong migrate/seed
            builder.UseSetting("ConnectionStrings:TourVoice", "Server=localhost;Database=unused;User=x;Password=y;");
            builder.UseSetting("Jwt:Key", JwtKey);
            builder.UseSetting("Jwt:Issuer", "TourVoiceTest");
            builder.UseSetting("Jwt:Audience", "TourVoiceTestClient");
            // Khong co MySQL trong test: thay bang stub de AccountStatusMiddleware khong cham CSDL.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAccountStatusService>();
                services.AddSingleton<IAccountStatusService>(new StubAccountStatus(!Locked));
            });
        }

        private sealed class StubAccountStatus(bool active) : IAccountStatusService
        {
            public Task<bool> IsActiveAsync(int userId, CancellationToken ct = default) => Task.FromResult(active);
        }
    }

    private static string Token(string role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        "TourVoiceTest", "TourVoiceTestClient",
        [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, role)],
        expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256)));

    private HttpClient Client(string? role = null)
    {
        var client = _factory.CreateClient();
        if (role is not null) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(role));
        return client;
    }

    private static async Task<JsonElement> Problem(HttpResponseMessage res)
    {
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Heartbeat_ThieuDeviceId_Tra400ProblemDetails_KemCodeVaTraceId()
    {
        var res = await Client().PostAsJsonAsync("/api/v1/presence/heartbeat", new { placeSlug = "cho-binh-tay", lang = "vi" });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var body = await Problem(res);
        Assert.Equal("invalid_device_id", body.GetProperty("code").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
        Assert.False(body.TryGetProperty("exception", out _));   // NFR-04: khong lo stack trace
    }

    [Fact]
    public async Task Leave_TuSendBeaconTextPlain_Tra204()
    {
        var content = new StringContent("""{"deviceId":"00000000-0000-4000-8000-000000000001"}""", Encoding.UTF8, "text/plain");
        var res = await Client().PostAsync("/api/v1/presence/leave", content);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task TokenConHan_NhungTaiKhoanDaBiKhoa_Tra403()
    {
        await using var factory = new Factory { Locked = true };
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Admin"));

        var res = await client.GetAsync("/api/v1/admin/presence");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("account_locked", (await Problem(res)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Me_KhongToken_Tra401()
    {
        var res = await Client().GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_TokenAdmin_Tra403_VIChiDanhChoCustomer()
    {
        var res = await Client("Admin").GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminPresence_Customer403_Admin200()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Customer").GetAsync("/api/v1/admin/presence")).StatusCode);

        var res = await Client("Admin").GetAsync("/api/v1/admin/presence");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("live", out _));
    }

    [Fact]
    public async Task Auth_Qua10LanMotPhut_Tra429()
    {
        // Factory rieng de bo dem rate limit khong anh huong test khac.
        await using var factory = new Factory();
        var client = factory.CreateClient();

        HttpResponseMessage last = null!;
        for (var i = 0; i < 11; i++)
            last = await client.PostAsJsonAsync("/api/v1/auth/login", new { });   // 400 validation, van tinh luot

        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
        Assert.Equal("rate_limited", (await Problem(last)).GetProperty("code").GetString());
    }

    // ---- admin: moi endpoint can JWT role Admin ----

    public static TheoryData<string> AdminGetEndpoints => new()
    {
        "/api/v1/admin/dashboard", "/api/v1/admin/stats/access", "/api/v1/admin/usage",
        "/api/v1/admin/places", "/api/v1/admin/places/1", "/api/v1/admin/users",
        "/api/v1/admin/languages", "/api/v1/admin/narrations/matrix",
    };

    [Theory]
    [MemberData(nameof(AdminGetEndpoints))]
    public async Task AdminEndpoints_KhongToken401_Customer403(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client().GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Customer").GetAsync(url)).StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/admin/places/1/publish")]
    [InlineData("POST", "/api/v1/admin/places/1/hide")]
    [InlineData("POST", "/api/v1/admin/places/1/translate")]
    [InlineData("POST", "/api/v1/admin/places/1/translations/en/approve")]
    [InlineData("DELETE", "/api/v1/admin/places/1")]
    [InlineData("POST", "/api/v1/admin/users/2/lock")]
    [InlineData("POST", "/api/v1/admin/users/2/unlock")]
    public async Task AdminWrites_Customer403(string method, string url)
    {
        var res = await Client("Customer").SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminCover_Customer403_VaKhongToken401()
    {
        using var form = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "file", "a.png" } };
        Assert.Equal(HttpStatusCode.Unauthorized, (await Client().PostAsync("/api/v1/admin/places/1/cover", form)).StatusCode);

        using var form2 = new MultipartFormDataContent { { new ByteArrayContent([1, 2, 3]), "file", "a.png" } };
        Assert.Equal(HttpStatusCode.Forbidden, (await Client("Customer").PostAsync("/api/v1/admin/places/1/cover", form2)).StatusCode);
    }

    // ---- /uploads ----

    [Fact]
    public async Task Uploads_PhucVuFileDaLuu_VaChan404ChoFileKhongCo()
    {
        Directory.CreateDirectory(Path.Combine(_factory.UploadsDir, "covers"));
        await File.WriteAllBytesAsync(Path.Combine(_factory.UploadsDir, "covers", "demo.png"), [0x89, 0x50, 0x4E, 0x47]);

        var ok = await Client().GetAsync("/uploads/covers/demo.png");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("image/png", ok.Content.Headers.ContentType?.MediaType);

        Assert.Equal(HttpStatusCode.NotFound, (await Client().GetAsync("/uploads/covers/none.png")).StatusCode);
    }

    [Fact]
    public async Task Uploads_KhongThoatKhoiThuMuc()
    {
        var res = await Client().GetAsync("/uploads/..%2f..%2fappsettings.json");
        Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
    }
}
