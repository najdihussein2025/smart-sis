using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Email;

namespace SmartSis.Api.Features.Notifications.Jobs;

/// <summary>Sends queued emails every 30 seconds. A failed send is retried up to EmailOutboxMessage.MaxAttempts.</summary>
public sealed class EmailOutboxJob(
    IServiceScopeFactory scopes,
    IEmailSender sender,
    TimeProvider clock,
    ILogger<EmailOutboxJob> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, clock);
        do
        {
            try
            {
                await SendBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Email outbox run failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    internal async Task SendBatchAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var batch = await repo.GetQueuedEmailsAsync(BatchSize, ct);
        if (batch.Count == 0) return;

        foreach (var message in batch)
        {
            try
            {
                await sender.SendAsync(message.ToEmail, message.Subject, message.Body, ct);
                message.MarkSent(clock.GetUtcNow().UtcDateTime);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Sending email {Id} failed (attempt {Attempt}).", message.Id, message.Attempts + 1);
                message.MarkFailedAttempt(ex.Message);
            }
        }

        await uow.SaveChangesAsync(ct);
    }
}
