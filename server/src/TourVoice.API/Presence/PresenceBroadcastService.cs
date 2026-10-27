using Microsoft.AspNetCore.SignalR;
using TourVoice.API.Hubs;

namespace TourVoice.API.Presence;

public class PresenceBroadcastService : BackgroundService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan BroadcastInterval = TimeSpan.FromSeconds(3);

    private readonly IPresenceTracker _tracker;
    private readonly IHubContext<PresenceHub> _hubContext;

    public PresenceBroadcastService(IPresenceTracker tracker, IHubContext<PresenceHub> hubContext)
    {
        _tracker = tracker;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _tracker.PurgeExpired(Ttl);
            var counts = _tracker.GetCounts();

            await _hubContext.Clients.Group("dashboard")
                .SendAsync("PresenceUpdated", counts, cancellationToken: stoppingToken);

            await Task.Delay(BroadcastInterval, stoppingToken);
        }
    }
}