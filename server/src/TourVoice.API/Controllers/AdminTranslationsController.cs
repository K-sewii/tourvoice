using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
[Route("api/v1/admin")]
public class AdminTranslationsController : ControllerBase
{
    private readonly INarrationPipelineService _service;
    public AdminTranslationsController(INarrationPipelineService service) => _service = service;

    /// <summary>"Dich phan thieu". Loi nghiep vu (404/422/429/502) do DomainExceptionHandler anh xa.</summary>
    [HttpPost("places/{placeId:int}/translate")]
    public async Task<IActionResult> Translate(int placeId) => Ok(await _service.TranslatePlaceAsync(placeId));

    [HttpPut("places/{placeId:int}/translations/{lang}")]
    public async Task<IActionResult> Update(int placeId, string lang, UpdateTranslationRequestDto request)
    {
        var result = await _service.UpdateTranslationAsync(placeId, lang, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("places/{placeId:int}/translations/{lang}/approve")]
    public async Task<IActionResult> Approve(int placeId, string lang) =>
        await _service.ApproveTranslationAsync(placeId, lang) ? NoContent() : NotFound();

    /// <summary>Ma tran Place x ngon ngu (SCR-A5); <c>filter</c>: all | draft | stale | missing.</summary>
    [HttpGet("narrations/matrix")]
    public async Task<IActionResult> Matrix([FromQuery] string? filter) => Ok(await _service.GetMatrixAsync(filter));
}
