using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

/// <summary>Tai khoan khach hang (PRD API: nhom Khach hang, JWT role Customer).</summary>
[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.CustomerRole)]
[Route("api/v1/me")]
public class MeController : ControllerBase
{
    private readonly ICustomerService _service;
    public MeController(ICustomerService service) => _service = service;

    private int UserId => User.RequiredUserId();

    [HttpGet]
    public Task<UserProfileDto> GetProfile() => _service.GetProfileAsync(UserId);

    [HttpGet("favorites")]
    public Task<List<FavoritePlaceDto>> GetFavorites([FromQuery] string lang = "vi") =>
        _service.GetFavoritesAsync(UserId, lang);

    [HttpPost("favorites/{placeId:int}")]
    public async Task<IActionResult> AddFavorite(int placeId)
    {
        await _service.AddFavoriteAsync(UserId, placeId);
        return NoContent();
    }

    [HttpDelete("favorites/{placeId:int}")]
    public async Task<IActionResult> RemoveFavorite(int placeId)
    {
        await _service.RemoveFavoriteAsync(UserId, placeId);
        return NoContent();
    }

    /// <summary>Noi da ghe + tien do x/y diem den (BR-10).</summary>
    [HttpGet("visits")]
    public Task<VisitsDto> GetVisits([FromQuery] string lang = "vi") => _service.GetVisitsAsync(UserId, lang);

    [HttpPost("visits/{placeId:int}")]
    public async Task<IActionResult> MarkVisited(int placeId)
    {
        await _service.MarkVisitedAsync(UserId, placeId);
        return NoContent();
    }

    [HttpGet("history")]
    public Task<List<ListenHistoryDto>> GetHistory([FromQuery] string lang = "vi") =>
        _service.GetHistoryAsync(UserId, lang);
}
