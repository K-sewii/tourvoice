using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
[Route("api/v1/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _service;
    public AdminUsersController(IAdminUserService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] string? role,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await _service.GetListAsync(search, role, page, pageSize));

    /// <summary>Tao Admin moi (tai khoan Admin chi tao bang seed hoac do Admin khac tao).</summary>
    [HttpPost]
    public async Task<IActionResult> CreateAdmin(CreateAdminRequestDto request) =>
        StatusCode(StatusCodes.Status201Created, await _service.CreateAdminAsync(request));

    [HttpPost("{id:int}/lock")]
    public async Task<IActionResult> Lock(int id)
    {
        var result = await _service.SetActiveAsync(id, false, User.RequiredUserId());
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:int}/unlock")]
    public async Task<IActionResult> Unlock(int id)
    {
        var result = await _service.SetActiveAsync(id, true, User.RequiredUserId());
        return result is null ? NotFound() : Ok(result);
    }
}
