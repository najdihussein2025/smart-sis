using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Notifications.Repositories;

public sealed class NotificationRepository(SisDbContext db) : INotificationRepository
{
    public void AddEmail(EmailOutboxMessage message) => db.Set<EmailOutboxMessage>().Add(message);

    public void AddNotification(Notification notification) => db.Set<Notification>().Add(notification);

    public Task<List<EmailOutboxMessage>> GetQueuedEmailsAsync(int take, CancellationToken ct) =>
        db.Set<EmailOutboxMessage>()
            .Where(m => m.Status == EmailStatus.Queued)
            .OrderBy(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
}
