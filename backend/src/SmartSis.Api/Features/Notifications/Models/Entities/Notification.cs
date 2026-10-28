using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Notifications.Models.Entities;

/// <summary>In-app notification (the bell).</summary>
public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    /// <summary>Frontend route to open, e.g. /student/attendance.</summary>
    public string? Link { get; set; }
    public DateTime? ReadAt { get; set; }
}
