using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Presence;

namespace TourVoice.API.Controllers;

/// <summary>
/// Dem nguoi truy cap (SD-04). Khach gui heartbeat HTTP thay vi giu WebSocket (ADR-10):
/// di qua moi mang va khong rot khi khoa man hinh. SignalR chi dung cho dashboard admin.
/// </summary>
[ApiController]
[Route("api/v1")]
public class PresenceController : ControllerBase
{
    public const string DeviceIdHeader = "X-Device-Id";

    private readonly IPresenceService _service;
    public PresenceController(IPresenceService service) => _service = service;

    /// <summary>Moi 15 giay khi trang dang hien (FR-19).</summary>
    [HttpPost("presence/heartbeat")]
    public async Task<IActionResult> Heartbeat([FromHeader(Name = DeviceIdHeader)] string? deviceId,
        HeartbeatRequestDto request, CancellationToken ct)
    {
        await _service.HeartbeatAsync(deviceId, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Goi bang navigator.sendBeacon luc pagehide. sendBeacon khong dat duoc header va gui
    /// text/plain (tranh preflight CORS), nen deviceId nam trong body JSON va doc thu cong.
    /// </summary>
    [HttpPost("presence/leave")]
    public async Task<IActionResult> Leave(CancellationToken ct)
    {
        string? deviceId = Request.Headers[DeviceIdHeader];
        if (string.IsNullOrEmpty(deviceId))
        {
            try
            {
                var body = await JsonSerializer.DeserializeAsync<LeaveBody>(Request.Body, JsonSerializerOptions.Web, ct);
                deviceId = body?.DeviceId;
            }
            catch (JsonException) { /* bo qua: beacon hong khong lam gi */ }
        }

        await _service.LeaveAsync(deviceId, ct);
        return NoContent();
    }

    [HttpGet("admin/presence")]
    [Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
    public PresenceSnapshotDto Snapshot() => _service.GetSnapshot();

    private sealed record LeaveBody(string? DeviceId);
}
