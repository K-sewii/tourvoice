using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TourVoice.API.Infrastructure;
using TourVoice.BLL.Presence;

namespace TourVoice.API.Hubs;

/// <summary>
/// Hub chi cho dashboard admin (BR-17): ket noi la vao nhom admins va nhan ngay so lieu hien tai,
/// sau do nhan "PresenceUpdated" moi khi so thiet bi doi. Khach khong ket noi hub (dung heartbeat HTTP).
/// </summary>
[Authorize(Roles = ClaimsPrincipalExtensions.AdminRole)]
public class PresenceHub(IPresenceService presence) : Hub
{
    public const string AdminsGroup = "admins";
    public const string UpdatedMethod = "PresenceUpdated";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        await Clients.Caller.SendAsync(UpdatedMethod, presence.GetSnapshot());
        await base.OnConnectedAsync();
    }
}

public class SignalRPresenceNotifier(IHubContext<PresenceHub> hub) : IPresenceNotifier
{
    public Task NotifyAsync(PresenceSnapshotDto snapshot, CancellationToken ct = default) =>
        hub.Clients.Group(PresenceHub.AdminsGroup).SendAsync(PresenceHub.UpdatedMethod, snapshot, ct);
}
