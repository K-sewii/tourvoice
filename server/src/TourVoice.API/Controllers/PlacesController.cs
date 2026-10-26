using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Route("api/v1/places")]
public class PlacesController : ControllerBase
{
    private readonly IPlaceService _service;
    public PlacesController(IPlaceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string lang = "vi") =>
        Ok(await _service.GetPublishedDestinationsAsync(lang));

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, [FromQuery] string lang = "vi")
    {
        var place = await _service.GetPlaceDetailAsync(slug, lang);
        return place is null ? NotFound() : Ok(place);
    }
}