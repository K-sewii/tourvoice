using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/places/{placeId:int}")]
public class AdminTranslationsController : ControllerBase
{
    private readonly INarrationPipelineService _service;
    public AdminTranslationsController(INarrationPipelineService service) => _service = service;

    [HttpPost("translate")]
    public async Task<IActionResult> Translate(int placeId)
    {
        try
        {
            var result = await _service.TranslatePlaceAsync(placeId);
            return Ok(result);
        }
        catch (PlaceNotFoundException)
        {
            return NotFound();
        }
        catch (MissingSourceNarrationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (QuotaExceededException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
        }
    }

    [HttpPost("translations/{lang}/approve")]
    public async Task<IActionResult> Approve(int placeId, string lang)
    {
        var ok = await _service.ApproveTranslationAsync(placeId, lang);
        return ok ? NoContent() : NotFound();
    }
}