using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
[Route("api/v1/admin/languages")]
public class AdminLanguagesController : ControllerBase
{
    private readonly ILanguageService _service;
    public AdminLanguagesController(ILanguageService service) => _service = service;

    /// <summary>Ca ngon ngu dang tat (GET /languages cong khai chi tra ngon ngu dang bat).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPut("{code}")]
    public async Task<IActionResult> Update(string code, UpdateLanguageRequestDto request)
    {
        var result = await _service.UpdateAsync(code, request);
        return result is null ? NotFound() : Ok(result);
    }
}
