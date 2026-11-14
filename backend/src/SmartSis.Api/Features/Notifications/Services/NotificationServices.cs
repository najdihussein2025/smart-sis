using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Notifications.Models.Dtos;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Notifications.Services;

public sealed class EmailQueue(INotificationRepository repo) : IEmailQueue
{
    public void Enqueue(string toEmail, string subject, string htmlBody) =>
        repo.AddEmail(new EmailOutboxMessage { ToEmail = toEmail, Subject = subject, Body = htmlBody });
}

public sealed class NotificationService(INotificationRepository repo) : INotificationService
{
    private const int TitleMax = 200;
    private const int BodyMax = 1000;

    public void Notify(int userId, NotificationType type, string title, string body, string? link = null) =>
        repo.AddNotification(Create(userId, type, title, body, link));

    public void NotifyMany(IEnumerable<(int UserId, string? Link)> recipients, NotificationType type, string title, string body) =>
        repo.AddNotifications(recipients.Select(r => Create(r.UserId, type, title, body, r.Link)).ToList());

    private static Notification Create(int userId, NotificationType type, string title, string body, string? link) => new()
    {
        UserId = userId, Type = type, Title = Cut(title, TitleMax), Body = Cut(body, BodyMax), Link = link,
    };

    /// <summary>Fits the text into the column (Notifications.Title 200, Body 1000), ending with "…" when cut.</summary>
    internal static string Cut(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";
}

/// <summary>The bell: the signed-in user's own notifications only. Someone else's notification id answers 404.</summary>
public sealed class NotificationInboxService(INotificationRepository repo, ICurrentUser me, IUnitOfWork uow, TimeProvider clock)
    : INotificationInboxService
{
    public Task<PagedResult<NotificationDto>> ListMineAsync(NotificationListQuery query, CancellationToken ct) =>
        repo.ListForUserAsync(me.UserId, query.UnreadOnly, new PageQuery(query.Page, query.PageSize), ct);

    public async Task<UnreadCountDto> UnreadCountAsync(CancellationToken ct) => new(await repo.CountUnreadAsync(me.UserId, ct));

    public async Task MarkReadAsync(int id, CancellationToken ct)
    {
        var n = await repo.FindForUserAsync(me.UserId, id, ct) ?? throw new NotFoundException("This notification was not found.");
        if (n.ReadAt is not null) return;
        n.ReadAt = clock.GetUtcNow().UtcDateTime;
        await uow.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct)
    {
        var unread = await repo.ListUnreadForUserAsync(me.UserId, ct);
        if (unread.Count == 0) return;
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var n in unread) n.ReadAt = now;
        await uow.SaveChangesAsync(ct);
    }
}
