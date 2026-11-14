using SmartSis.Api.Features.Notifications.Models.Dtos;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Notifications.Interfaces;

public interface INotificationRepository
{
    void AddEmail(EmailOutboxMessage message);
    void AddNotification(Notification notification);
    void AddNotifications(IEnumerable<Notification> notifications);
    Task<List<EmailOutboxMessage>> GetQueuedEmailsAsync(int take, CancellationToken ct);

    /// <summary>Queued emails whose retry time has come. <paramref name="cutoffs"/>[n] comes from <c>EmailRetryPolicy.DueCutoffs</c>.</summary>
    Task<List<EmailOutboxMessage>> GetDueEmailsAsync(IReadOnlyList<DateTime> cutoffs, int take, CancellationToken ct);

    Task<PagedResult<NotificationDto>> ListForUserAsync(int userId, bool unreadOnly, PageQuery page, CancellationToken ct);
    Task<int> CountUnreadAsync(int userId, CancellationToken ct);
    Task<Notification?> FindForUserAsync(int userId, int id, CancellationToken ct);
    Task<List<Notification>> ListUnreadForUserAsync(int userId, CancellationToken ct);
}
