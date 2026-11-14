using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Announcements.Models.Dtos;
using SmartSis.Api.Features.Announcements.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Announcements.Interfaces;

/// <summary>Announcements (PB-12, rule C1). The user always comes from the token.</summary>
public interface IAnnouncementService
{
    /// <summary>My feed: published, not expired, meant for me; plus my own scheduled ones.</summary>
    Task<PagedResult<AnnouncementDto>> FeedAsync(AnnouncementListQuery query, CancellationToken ct);

    /// <summary>Admin: every announcement, any status.</summary>
    Task<PagedResult<AnnouncementDto>> ListAllAsync(AnnouncementListQuery query, CancellationToken ct);

    /// <summary>Instructor: the ones I posted, any status.</summary>
    Task<PagedResult<AnnouncementDto>> ListMineAsync(AnnouncementListQuery query, CancellationToken ct);

    /// <summary>Instructor: sections I teach in the current term, for the post form.</summary>
    Task<List<AnnouncementSectionDto>> MySectionsAsync(CancellationToken ct);

    Task<AnnouncementDto> CreateAsync(SaveAnnouncementRequest request, CancellationToken ct);
    Task<AnnouncementDto> UpdateAsync(int id, SaveAnnouncementRequest request, CancellationToken ct);
    Task<AnnouncementDto> CreateForSectionAsync(SaveSectionAnnouncementRequest request, CancellationToken ct);
    Task<AnnouncementDto> UpdateForSectionAsync(int id, SaveSectionAnnouncementRequest request, CancellationToken ct);

    /// <summary>Admin: any announcement. Instructor: only their own (403 otherwise).</summary>
    Task DeleteAsync(int id, CancellationToken ct);

    /// <summary>
    /// Creates the in-app notifications of announcements that are published, not expired, published in the last
    /// <paramref name="lookback"/> and not notified yet. Run by AnnouncementPublishJob for scheduled posts. Returns how many announcements were fanned out (one with no audience is checked again on the next run).
    /// </summary>
    Task<int> NotifyDueAsync(TimeSpan lookback, CancellationToken ct);
}

/// <summary>Who is looking: decides which announcements are in their feed and which they may manage.</summary>
public sealed record AnnouncementViewer(int UserId, UserRole Role, int? MajorId);

/// <summary>Facts about a section, for an instructor's post.</summary>
public sealed record AnnouncementSectionInfo(int Id, int? InstructorId, string Label);

public interface IAnnouncementRepository
{
    Task<PagedResult<AnnouncementDto>> FeedAsync(AnnouncementViewer viewer, DateTime now, PageQuery page, CancellationToken ct);
    Task<PagedResult<AnnouncementDto>> ListAsync(AnnouncementViewer viewer, int? authorId, AnnouncementStatus? status, DateTime now, PageQuery page, CancellationToken ct);
    Task<AnnouncementDto?> GetDtoAsync(int id, AnnouncementViewer viewer, DateTime now, CancellationToken ct);
    Task<Announcement?> FindAsync(int id, CancellationToken ct);
    Task<int?> GetStudentMajorIdAsync(int userId, CancellationToken ct);
    Task<bool> MajorExistsAsync(int majorId, CancellationToken ct);
    Task<AnnouncementSectionInfo?> GetSectionAsync(int sectionId, CancellationToken ct);
    Task<List<AnnouncementSectionDto>> ListCurrentSectionsTaughtByAsync(int instructorId, CancellationToken ct);

    /// <summary>Active users the announcement is meant for, except its author.</summary>
    Task<List<(int UserId, UserRole Role)>> GetAudienceAsync(Announcement announcement, CancellationToken ct);

    Task<bool> IsNotifiedAsync(int announcementId, CancellationToken ct);

    /// <summary>Live announcements published after <paramref name="since"/> that have no notification yet.</summary>
    Task<List<Announcement>> ListDueForNotificationAsync(DateTime since, DateTime now, CancellationToken ct);

    void Add(Announcement announcement);
    void Remove(Announcement announcement);
}
