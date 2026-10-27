using Microsoft.AspNetCore.SignalR;
using TourVoice.API.Presence;

namespace TourVoice.API.Hubs;

public class PresenceHub : Hub
{
    private const string DashboardGroup = "dashboard";
    private readonly IPresenceTracker _tracker;

    public PresenceHub(IPresenceTracker tracker)
    {
        _tracker = tracker;
    }

    public Task Heartbeat(string placeSlug)
    {
        _tracker.Heartbeat(Context.ConnectionId, placeSlug);
        return Task.CompletedTask;
    }

    public Task SubscribeDashboard()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _tracker.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}