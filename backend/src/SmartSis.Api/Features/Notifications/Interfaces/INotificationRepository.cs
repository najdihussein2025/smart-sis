using SmartSis.Api.Features.Notifications.Models.Entities;

namespace SmartSis.Api.Features.Notifications.Interfaces;

public interface INotificationRepository
{
    void AddEmail(EmailOutboxMessage message);
    void AddNotification(Notification notification);
    Task<List<EmailOutboxMessage>> GetQueuedEmailsAsync(int take, CancellationToken ct);
}
