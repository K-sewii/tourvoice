using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/places")]
public class AdminPlacesController : ControllerBase
{
    private readonly IAdminPlaceService _service;
    public AdminPlacesController(IAdminPlaceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _service.GetListAsync(page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var place = await _service.GetByIdAsync(id);
        return place is null ? NotFound() : Ok(place);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePlaceRequestDto request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (SlugAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePlaceRequestDto request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id) =>
        await _service.PublishAsync(id) ? NoContent() : NotFound();

    [HttpPost("{id:int}/hide")]
    public async Task<IActionResult> Hide(int id) =>
        await _service.HideAsync(id) ? NoContent() : NotFound();
}