using Microsoft.AspNetCore.Mvc;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Dtos;
using TourVoice.BLL.Services;

namespace TourVoice.API.Controllers;

[ApiController]
[Route("api/v1/listen-events")]
public class ListenEventsController : ControllerBase
{
    private readonly IListenService _service;
    public ListenEventsController(IListenService service) => _service = service;

    /// <summary>Ghi luot nghe >= 10 giay (BR-09); an danh voi khach, gan tai khoan neu co JWT.</summary>
    [HttpPost]
    public async Task<IActionResult> Record(ListenEventRequestDto request)
    {
        await _service.RecordAsync(request, User.UserId());
        return NoContent();
    }
}
