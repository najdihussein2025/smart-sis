using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Notifications.Interfaces;

/// <summary>Create in-app notifications. Does not save; the caller's unit of work commits them.</summary>
public interface INotificationService
{
    void Notify(int userId, NotificationType type, string title, string body, string? link = null);

    /// <summary>One notification per recipient, each with its own link (e.g. the announcements page of their role).</summary>
    void NotifyMany(IEnumerable<(int UserId, string? Link)> recipients, NotificationType type, string title, string body);
}

/// <summary>The signed-in user's own notifications (the bell). The user always comes from the token.</summary>
public interface INotificationInboxService
{
    Task<PagedResult<NotificationDto>> ListMineAsync(NotificationListQuery query, CancellationToken ct);
    Task<UnreadCountDto> UnreadCountAsync(CancellationToken ct);
    Task MarkReadAsync(int id, CancellationToken ct);
    Task MarkAllReadAsync(CancellationToken ct);
}
