using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Notifications.Models.Dtos;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Notifications.Repositories;

public sealed class NotificationRepository(SisDbContext db) : INotificationRepository
{
    public void AddEmail(EmailOutboxMessage message) => db.Set<EmailOutboxMessage>().Add(message);

    public void AddNotification(Notification notification) => db.Set<Notification>().Add(notification);

    public void AddNotifications(IEnumerable<Notification> notifications) => db.Set<Notification>().AddRange(notifications);

    public Task<List<EmailOutboxMessage>> GetQueuedEmailsAsync(int take, CancellationToken ct) =>
        db.Set<EmailOutboxMessage>()
            .Where(m => m.Status == EmailStatus.Queued)
            .OrderBy(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public Task<List<EmailOutboxMessage>> GetDueEmailsAsync(IReadOnlyList<DateTime> cutoffs, int take, CancellationToken ct)
    {
        // (Attempts = 0 AND UpdatedAt <= c0) OR (Attempts = 1 AND UpdatedAt <= c1) OR …
        var m = Expression.Parameter(typeof(EmailOutboxMessage), "m");
        Expression due = Expression.Constant(false);
        for (var n = 0; n < cutoffs.Count; n++)
            due = Expression.OrElse(due, Expression.AndAlso(
                Expression.Equal(Expression.Property(m, nameof(EmailOutboxMessage.Attempts)), Expression.Constant(n)),
                Expression.LessThanOrEqual(Expression.Property(m, nameof(EmailOutboxMessage.UpdatedAt)), Expression.Constant(cutoffs[n]))));

        return db.Set<EmailOutboxMessage>()
            .Where(x => x.Status == EmailStatus.Queued)
            .Where(Expression.Lambda<Func<EmailOutboxMessage, bool>>(due, m))
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<NotificationDto>> ListForUserAsync(int userId, bool unreadOnly, PageQuery page, CancellationToken ct)
    {
        var q = db.Set<Notification>().AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly) q = q.Where(n => n.ReadAt == null);
        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip(page.Skip).Take(page.Take)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.Link, n.CreatedAt, n.ReadAt))
            .ToListAsync(ct);
        return new PagedResult<NotificationDto>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken ct) =>
        db.Set<Notification>().CountAsync(n => n.UserId == userId && n.ReadAt == null, ct);

    public Task<Notification?> FindForUserAsync(int userId, int id, CancellationToken ct) =>
        db.Set<Notification>().FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

    public Task<List<Notification>> ListUnreadForUserAsync(int userId, CancellationToken ct) =>
        db.Set<Notification>().Where(n => n.UserId == userId && n.ReadAt == null).ToListAsync(ct);
}
