using TourVoice.BLL.Presence;

namespace TourVoice.API.Infrastructure;

/// <summary>Moi 5 giay loai thiet bi im lang qua TTL 45 giay (mat mang, het pin): toi da 45 + 5 giay.</summary>
public sealed class PresenceSweeper(IServiceScopeFactory scopes, ILogger<PresenceSweeper> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IPresenceService>().SweepAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // CSDL tam loi khong duoc lam chet vong quet; lan sau thu lai.
                logger.LogWarning(ex, "Presence sweep failed");
            }
        }
    }
}
