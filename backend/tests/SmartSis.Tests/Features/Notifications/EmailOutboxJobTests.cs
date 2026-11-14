using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Notifications.Jobs;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Notifications.Repositories;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Data.Interceptors;
using SmartSis.Api.Shared.Email;

namespace SmartSis.Tests.Features.Notifications;

/// <summary>T48: EmailOutboxJob sends queued mail, backs off after a failure and gives up after MaxAttempts (rule C2).</summary>
public sealed class EmailOutboxJobTests : IAsyncDisposable
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 2, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeSender _sender = new();
    private readonly ServiceProvider _services;
    private readonly EmailOutboxJob _job;

    public EmailOutboxJobTests()
    {
        var dbName = $"Outbox-{Guid.NewGuid():N}";
        _services = new ServiceCollection()
            .AddSingleton<TimeProvider>(_clock)
            .AddScoped<AuditInterceptor>()
            .AddDbContext<SisDbContext>((sp, o) => o.UseInMemoryDatabase(dbName).AddInterceptors(sp.GetRequiredService<AuditInterceptor>()))
            .AddScoped<IUnitOfWork, UnitOfWork>()
            .AddScoped<INotificationRepository, NotificationRepository>()
            .BuildServiceProvider();
        _job = new EmailOutboxJob(_services.GetRequiredService<IServiceScopeFactory>(), _sender, _clock, NullLogger<EmailOutboxJob>.Instance);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();

    private async Task QueueAsync(string to = "student@smartsis.local")
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SisDbContext>();
        db.Add(new EmailOutboxMessage { ToEmail = to, Subject = "Absence warning", Body = "<p>Simulated</p>" });
        await db.SaveChangesAsync();
    }

    private async Task<EmailOutboxMessage> MessageAsync()
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SisDbContext>().Set<EmailOutboxMessage>().AsNoTracking().SingleAsync();
    }

    [Fact]
    public async Task SendBatch_Success_MarksSent()
    {
        await QueueAsync();
        await _job.SendBatchAsync(default);

        var m = await MessageAsync();
        Assert.Equal(EmailStatus.Sent, m.Status);
        Assert.Equal(1, m.Attempts);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, m.SentAt);
        Assert.Equal(1, _sender.Calls);
    }

    [Fact]
    public async Task SendBatch_AfterFailure_WaitsForBackoffBeforeRetrying()
    {
        _sender.Fail = true;
        await QueueAsync();

        await _job.SendBatchAsync(default);               // attempt 1 fails
        Assert.Equal(1, _sender.Calls);

        _clock.Advance(TimeSpan.FromSeconds(30));         // next run: still inside the 1-minute wait
        await _job.SendBatchAsync(default);
        Assert.Equal(1, _sender.Calls);

        _clock.Advance(TimeSpan.FromSeconds(30));         // 1 minute after the failure
        await _job.SendBatchAsync(default);
        Assert.Equal(2, _sender.Calls);

        _clock.Advance(TimeSpan.FromSeconds(90));         // 2-minute wait after the second failure isn't over
        await _job.SendBatchAsync(default);
        Assert.Equal(2, _sender.Calls);

        _sender.Fail = false;
        _clock.Advance(TimeSpan.FromSeconds(30));
        await _job.SendBatchAsync(default);
        Assert.Equal(3, _sender.Calls);
        Assert.Equal(EmailStatus.Sent, (await MessageAsync()).Status);
    }

    [Fact]
    public async Task SendBatch_FailsMaxAttempts_MarksFailedAndStops()
    {
        _sender.Fail = true;
        await QueueAsync();

        for (var i = 0; i < 10; i++)
        {
            await _job.SendBatchAsync(default);
            _clock.Advance(TimeSpan.FromMinutes(10));
        }

        var m = await MessageAsync();
        Assert.Equal(EmailStatus.Failed, m.Status);
        Assert.Equal(EmailOutboxMessage.MaxAttempts, m.Attempts);
        Assert.Equal(EmailOutboxMessage.MaxAttempts, _sender.Calls);
        Assert.Equal("SMTP unavailable (simulated).", m.LastError);
    }

    [Fact]
    public async Task SendBatch_OneFails_OthersStillSent()
    {
        _sender.FailFor = "broken@smartsis.local";
        await QueueAsync("broken@smartsis.local");
        await QueueAsync("student@smartsis.local");

        await _job.SendBatchAsync(default);

        await using var scope = _services.CreateAsyncScope();
        var all = await scope.ServiceProvider.GetRequiredService<SisDbContext>().Set<EmailOutboxMessage>().AsNoTracking().ToListAsync();
        Assert.Equal(EmailStatus.Queued, all.Single(m => m.ToEmail == "broken@smartsis.local").Status);
        Assert.Equal(EmailStatus.Sent, all.Single(m => m.ToEmail == "student@smartsis.local").Status);
    }

    private sealed class FakeSender : IEmailSender
    {
        public bool Fail { get; set; }
        public string? FailFor { get; set; }
        public int Calls { get; private set; }

        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Calls++;
            if (Fail || to == FailFor) throw new InvalidOperationException("SMTP unavailable (simulated).");
            return Task.CompletedTask;
        }
    }
}
