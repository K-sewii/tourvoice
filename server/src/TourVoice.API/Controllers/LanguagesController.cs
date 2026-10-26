using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Route("api/v1/languages")]
public class LanguagesController : ControllerBase
{
    private readonly ILanguageService _service;
    public LanguagesController(ILanguageService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetLanguagesAsync());
}