using Microsoft.AspNetCore.Mvc;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    public AuthController(IAuthService service) => _service = service;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        try
        {
            var result = await _service.RegisterAsync(request);
            return StatusCode(201, result);
        }
        catch (EmailAlreadyExistsException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var result = await _service.LoginAsync(request);
        return result is null ? Unauthorized(new { message = "Email hoac mat khau khong dung." }) : Ok(result);
    }
}