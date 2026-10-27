using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Controllers;

namespace TourVoice.API.Infrastructure;

/// <summary>
/// Gioi han toc do (PRD muc 8): 120 request/phut moi deviceId, tran 1.200/phut moi IP de ca doan
/// dung chung Wi-Fi khong bi chan; dang nhap/dang ky 10 lan/phut moi IP. Vuot nguong -> 429 ProblemDetails.
/// </summary>
public static class RateLimits
{
    public const string AuthPolicy = "auth";
    public const int PerDevicePerMinute = 120;
    public const int PerIpPerMinute = 1200;
    public const int AuthPerIpPerMinute = 10;

    public static IServiceCollection AddTourVoiceRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(o =>
        {
            o.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(http =>
                {
                    var device = http.Request.Headers[PresenceController.DeviceIdHeader].ToString();
                    // Khong co deviceId (vd. sendBeacon) thi chi chiu tran theo IP.
                    return string.IsNullOrEmpty(device)
                        ? RateLimitPartition.GetNoLimiter("no-device")
                        : RateLimitPartition.GetFixedWindowLimiter($"d:{device}", _ => Window(PerDevicePerMinute));
                }),
                PartitionedRateLimiter.Create<HttpContext, string>(http =>
                    RateLimitPartition.GetFixedWindowLimiter($"ip:{ClientIp(http)}", _ => Window(PerIpPerMinute))));

            o.AddPolicy(AuthPolicy, http =>
                RateLimitPartition.GetFixedWindowLimiter($"auth:{ClientIp(http)}", _ => Window(AuthPerIpPerMinute)));

            o.OnRejected = async (ctx, ct) =>
            {
                var http = ctx.HttpContext;
                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    http.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();

                await http.RequestServices.GetRequiredService<IProblemDetailsService>().TryWriteAsync(new()
                {
                    HttpContext = http,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Quá nhiều yêu cầu, vui lòng thử lại sau.",
                        Extensions = { ["code"] = "rate_limited" },
                    },
                });
            };
        });

    private static FixedWindowRateLimiterOptions Window(int permits) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    };

    private static string ClientIp(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
