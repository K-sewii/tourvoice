using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public class MeController : ControllerBase
{
    private readonly ICustomerService _service;
    public MeController(ICustomerService service) => _service = service;

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetProfile() =>
        Ok(await _service.GetProfileAsync(CurrentUserId));

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites() =>
        Ok(await _service.GetFavoritesAsync(CurrentUserId));

    [HttpPost("favorites/{placeId:int}")]
    public async Task<IActionResult> AddFavorite(int placeId)
    {
        await _service.AddFavoriteAsync(CurrentUserId, placeId);
        return NoContent();
    }

    [HttpDelete("favorites/{placeId:int}")]
    public async Task<IActionResult> RemoveFavorite(int placeId)
    {
        await _service.RemoveFavoriteAsync(CurrentUserId, placeId);
        return NoContent();
    }

    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits() =>
        Ok(await _service.GetVisitsAsync(CurrentUserId));

    [HttpPost("visits/{placeId:int}")]
    public async Task<IActionResult> MarkVisited(int placeId)
    {
        await _service.MarkVisitedAsync(CurrentUserId, placeId);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory() =>
        Ok(await _service.GetHistoryAsync(CurrentUserId));
}