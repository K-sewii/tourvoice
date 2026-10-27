using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

/// <summary>Noi dung cong khai (PRD API: nhom Cong khai). Khong can dang nhap.</summary>
[ApiController]
[Route("api/v1")]
public class PlacesController : ControllerBase
{
    private readonly IPlaceService _service;
    public PlacesController(IPlaceService service) => _service = service;

    [HttpGet("places")]
    public Task<List<PlaceSummaryDto>> GetDestinations([FromQuery] string lang = "vi") =>
        _service.GetPublishedDestinationsAsync(lang);

    /// <summary>Chi tiet (SD-01). Kem JWT Customer thi co isFavorite/isVisited.</summary>
    [HttpGet("places/{slug}")]
    public Task<PlaceDetailDto> GetBySlug(string slug, [FromQuery] string lang = "vi") =>
        _service.GetPlaceDetailAsync(slug, lang, User.CustomerId());

    [HttpGet("places/{slug}/foods")]
    public Task<List<FoodPlaceSummaryDto>> GetFoods(string slug, [FromQuery] string lang = "vi") =>
        _service.GetFoodsOfDestinationAsync(slug, lang);

    /// <summary>Kham pha (SCR-07): category = Heritage|Market|FoodStreet|Park|food; q tim khong dau.</summary>
    [HttpGet("explore")]
    public Task<List<ExploreItemDto>> Explore([FromQuery] string lang = "vi",
        [FromQuery] string? category = null, [FromQuery] string? q = null) =>
        _service.ExploreAsync(lang, category, q);

    [HttpGet("map/markers")]
    public Task<List<MapMarkerDto>> GetMapMarkers([FromQuery] string lang = "vi") =>
        _service.GetMapMarkersAsync(lang);
}
