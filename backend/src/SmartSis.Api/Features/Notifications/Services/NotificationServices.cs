using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Notifications.Models.Entities;

namespace SmartSis.Api.Features.Notifications.Services;

public sealed class EmailQueue(INotificationRepository repo) : IEmailQueue
{
    public void Enqueue(string toEmail, string subject, string htmlBody) =>
        repo.AddEmail(new EmailOutboxMessage { ToEmail = toEmail, Subject = subject, Body = htmlBody });
}

public sealed class NotificationService(INotificationRepository repo) : INotificationService
{
    public void Notify(int userId, NotificationType type, string title, string body, string? link = null) =>
        repo.AddNotification(new Notification { UserId = userId, Type = type, Title = title, Body = body, Link = link });
}
