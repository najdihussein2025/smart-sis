using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Api.Features.Announcements.Rules;

/// <summary>Pure rules for when an announcement shows and where its notification points (PB-12, rule C1).</summary>
public static class AnnouncementSchedule
{
    /// <summary>Scopes an admin may choose (rule C1). Section is the instructor's scope.</summary>
    public static bool IsAdminScope(AnnouncementScope scope) =>
        scope is AnnouncementScope.All or AnnouncementScope.Students or AnnouncementScope.Instructors or AnnouncementScope.Major;

    /// <summary>
    /// The PublishAt to store. A future time schedules it. An empty or past time means "now", except that an announcement
    /// that is already published keeps its original date (editing a typo doesn't move it to the top of the feed).
    /// </summary>
    public static DateTime ResolvePublishAt(DateTime? requested, DateTime? current, DateTime utcNow)
    {
        if (requested is { } r && r > utcNow) return r;
        if (current is { } c && c <= utcNow) return c;
        return utcNow;
    }

    public static AnnouncementStatus StatusAt(DateTime publishAt, DateTime? expiresAt, DateTime utcNow) =>
        publishAt > utcNow ? AnnouncementStatus.Scheduled
        : expiresAt is { } e && e <= utcNow ? AnnouncementStatus.Expired
        : AnnouncementStatus.Live;

    /// <summary>Error for ExpiresAt, or null when it is fine: it must be in the future and after PublishAt.</summary>
    public static string? CheckExpiry(DateTime publishAt, DateTime? expiresAt, DateTime utcNow) =>
        expiresAt is not { } e ? null
        : e <= utcNow ? "The expiry must be in the future."
        : e <= publishAt ? "The expiry must be after the publish time."
        : null;

    /// <summary>Query string that ends every notification link of announcement <paramref name="id"/>.</summary>
    public static string LinkMarker(int id) => $"?announcement={id}";

    /// <summary>Where the bell sends this user: their role's announcements page, with the announcement highlighted.</summary>
    public static string LinkFor(UserRole role, int id) => role switch
    {
        UserRole.Student => "/student/announcements",
        UserRole.Instructor => "/instructor/announcements",
        _ => "/admin/announcements",
    } + LinkMarker(id);
}
