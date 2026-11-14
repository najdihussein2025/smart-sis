namespace SmartSis.Api.Features.Announcements.Enums;

/// <summary>Computed from PublishAt / ExpiresAt at request time, never stored.</summary>
public enum AnnouncementStatus
{
    /// <summary>PublishAt is in the future. Only the author (and admins in the manage list) see it.</summary>
    Scheduled,

    /// <summary>Published and not expired: in the audience's feed.</summary>
    Live,

    /// <summary>ExpiresAt has passed. Kept for the manage list, gone from the feed.</summary>
    Expired,
}
