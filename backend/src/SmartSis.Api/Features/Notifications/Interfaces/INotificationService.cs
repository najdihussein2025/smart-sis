using SmartSis.Api.Features.Notifications.Enums;

namespace SmartSis.Api.Features.Notifications.Interfaces;

/// <summary>Create an in-app notification. Does not save; the caller's unit of work commits it.</summary>
public interface INotificationService
{
    void Notify(int userId, NotificationType type, string title, string body, string? link = null);

    // TODO(phase-6): list mine, unread count, mark read.
}
