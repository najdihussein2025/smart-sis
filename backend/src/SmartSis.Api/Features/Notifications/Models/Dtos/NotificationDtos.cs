using SmartSis.Api.Features.Notifications.Enums;

namespace SmartSis.Api.Features.Notifications.Models.Dtos;

/// <summary>An in-app notification of the signed-in user.</summary>
/// <param name="Id">Notification id.</param>
/// <param name="Type">What it is about, e.g. "Announcement".</param>
/// <param name="Title">Short title.</param>
/// <param name="Body">Text, up to 1000 characters.</param>
/// <param name="Link">Frontend route to open, e.g. "/student/announcements?announcement=4", or null.</param>
/// <param name="CreatedAt">When it was created (UTC).</param>
/// <param name="ReadAt">When the user read it (UTC), or null when unread.</param>
public sealed record NotificationDto(
    int Id, NotificationType Type, string Title, string Body, string? Link, DateTime CreatedAt, DateTime? ReadAt);

/// <summary>Number of unread notifications (the bell badge).</summary>
/// <param name="Count">Unread notifications.</param>
public sealed record UnreadCountDto(int Count);

/// <summary>Page through my notifications, newest first.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">1–100, default 20.</param>
/// <param name="UnreadOnly">Only unread ones.</param>
public sealed record NotificationListQuery(int Page = 1, int PageSize = 20, bool UnreadOnly = false);
