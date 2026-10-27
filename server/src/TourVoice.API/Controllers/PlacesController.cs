using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Route("api/v1/places")]
public class PlacesController : ControllerBase
{
    private readonly IPlaceService _service;
    private readonly IListenService _listenService;

    public PlacesController(IPlaceService service, IListenService listenService)
    {
        _service = service;
        _listenService = listenService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string lang = "vi") =>
        Ok(await _service.GetPublishedDestinationsAsync(lang));

    [HttpGet("foods")]
    public async Task<IActionResult> GetAllFoods([FromQuery] string lang = "vi") =>
        Ok(await _service.GetPublishedFoodsAsync(lang));

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, [FromQuery] string lang = "vi")
    {
        var place = await _service.GetPlaceDetailAsync(slug, lang);
        return place is null ? NotFound() : Ok(place);
    }

    [HttpPost("{slug}/listen")]
    public async Task<IActionResult> RecordListen(string slug, [FromBody] RecordListenRequestDto request)
    {
        int? userId = null;
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (idClaim is not null && int.TryParse(idClaim, out var parsedId)) userId = parsedId;

        var ok = await _listenService.RecordListenAsync(slug, request, userId);
        return ok ? NoContent() : NotFound();
    }
}