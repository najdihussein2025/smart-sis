using SmartSis.Api.Features.Announcements.Interfaces;

namespace SmartSis.Api.Features.Announcements.Jobs;

/// <summary>
/// Every minute: notifies the audience of scheduled announcements whose PublishAt has come (posts that are live when saved
/// are notified at once by the service). An announcement counts as notified once any of its notifications exists, so a
/// restart never notifies twice. Only posts published in the last 7 days are considered.
/// </summary>
public sealed class AnnouncementPublishJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<AnnouncementPublishJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan Lookback = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var count = await scope.ServiceProvider.GetRequiredService<IAnnouncementService>().NotifyDueAsync(Lookback, stoppingToken);
                if (count > 0) logger.LogInformation("Notified the audience of {Count} scheduled announcement(s).", count);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Announcement publish run failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
