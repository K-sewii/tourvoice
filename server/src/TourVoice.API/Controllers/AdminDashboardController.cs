using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
[Route("api/v1/admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _service;
    public AdminDashboardController(IAdminDashboardService service) => _service = service;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() => Ok(await _service.GetDashboardAsync());

    [HttpGet("stats/access")]
    public async Task<IActionResult> AccessStats([FromQuery] int days = 14) => Ok(await _service.GetAccessStatsAsync(days));

    [HttpGet("usage")]
    public async Task<IActionResult> Usage() => Ok(await _service.GetUsageAsync());
}
