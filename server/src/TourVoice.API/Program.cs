using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using TourVoice.API.Hubs;
using TourVoice.API.Infrastructure;
using TourVoice.BLL;
using TourVoice.BLL.Presence;
using TourVoice.BLL.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Program.cs chi biet BLL; AddBusinessLayer tu goi AddDataAccessLayer (ADR-01).
builder.Services.AddBusinessLayer(builder.Configuration);

// Moi loi -> RFC 7807 ProblemDetails kem traceId (NFR-10).
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddTourVoiceRateLimiting();

// Anh bia admin tai len: luu dia, phuc vu o /uploads (mount volume khi chay docker).
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
var uploadsRoot = LocalFileStorage.ResolveRoot(builder.Configuration, builder.Environment.ContentRootPath);
Directory.CreateDirectory(uploadsRoot);

builder.Services.AddSignalR();
builder.Services.AddSingleton<IPresenceNotifier, SignalRPresenceNotifier>();
builder.Services.AddHostedService<PresenceSweeper>();

// Chay sau Cloudflare Tunnel / nginx / docker: X-Forwarded-For cho biet IP khach that, neu khong
// rate limit theo IP se sup ve mot partition (IP cua proxy). Chi tin loopback (mac dinh) va
// dai IP noi bo cua Docker; request tu Internet khong di qua proxy se bi bo qua header.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var cidr in new[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" })
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
});

const string CorsPolicy = "Web";
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy
        .SetIsOriginAllowed(origin =>
            corsOrigins.Contains(origin) ||
            (Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && uri.Host.EndsWith(".trycloudflare.com", StringComparison.OrdinalIgnoreCase)))
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key phai co it nhat 32 byte (HS256, ADR-06).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
        // Trinh duyet khong dat header cho WebSocket: SignalR gui token qua ?access_token=.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Phai dat truoc moi middleware doc scheme/IP (CORS, rate limiter, redirect).
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // PRD: Swagger o /swagger de frontend va nguoi kiem thu dung chung dac ta.
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "TourVoice API v1"));
    await app.Services.InitializeDatabaseAsync();
}
else if (app.Configuration.GetValue<bool>("Database:InitializeOnStartup"))
{
    // Deploy (cd.yml): migrate + seed lan dau ma khong mo Swagger. Tat sau khi dung de khong chay lai seed.
    await app.Services.InitializeDatabaseAsync();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = LocalFileStorage.RequestPath,
});

app.UseCors(CorsPolicy);
app.UseAuthentication();
// BR-12: token con han nhung tai khoan da bi khoa thi van phai bi chan.
app.UseMiddleware<AccountStatusMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health").DisableRateLimiting();
app.MapHub<PresenceHub>("/hubs/presence");

app.Run();

// Cho WebApplicationFactory/NetArchTest tham chieu.
public partial class Program;
