using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
[Route("api/v1/admin/places")]
public class AdminPlacesController : ControllerBase
{
    private readonly IAdminPlaceService _service;
    public AdminPlacesController(IAdminPlaceService service) => _service = service;

    /// <summary>Cay diem den -> mon (SCR-A3), phan trang theo diem den; <c>status</c> loc theo trang thai.</summary>
    [HttpGet]
    public async Task<IActionResult> GetTree([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _service.GetTreeAsync(status, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var place = await _service.GetByIdAsync(id);
        return place is null ? NotFound() : Ok(place);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePlaceRequestDto request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdatePlaceRequestDto request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>BR-08: 422 publish_preconditions_failed kem <c>missing</c> neu thieu dieu kien.</summary>
    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id) =>
        await _service.PublishAsync(id) ? NoContent() : NotFound();

    [HttpPost("{id:int}/hide")]
    public async Task<IActionResult> Hide(int id) =>
        await _service.HideAsync(id) ? NoContent() : NotFound();

    /// <summary>BR-13: chi xoa Draft khong con mon con.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await _service.DeleteAsync(id) ? NoContent() : NotFound();

    /// <summary>Tai anh bia (multipart, truong <c>file</c>; jpg/png/webp toi da 5 MB).</summary>
    [HttpPost("{id:int}/cover")]
    [RequestSizeLimit(AdminPlaceService.MaxCoverBytes + 64 * 1024)]
    public async Task<IActionResult> UploadCover(int id, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var result = await _service.SetCoverAsync(id, stream, file.Length);
        return result is null ? NotFound() : Ok(result);
    }
}
