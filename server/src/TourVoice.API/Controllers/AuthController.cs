using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimits.AuthPolicy)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    public AuthController(IAuthService service) => _service = service;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request) =>
        StatusCode(StatusCodes.Status201Created, await _service.RegisterAsync(request));

    [HttpPost("login")]
    public Task<AuthResponseDto> Login(LoginRequestDto request) => _service.LoginAsync(request);
}
