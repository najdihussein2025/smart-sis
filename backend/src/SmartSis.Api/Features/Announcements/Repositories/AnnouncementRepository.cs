using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Announcements.Interfaces;
using SmartSis.Api.Features.Announcements.Models.Dtos;
using SmartSis.Api.Features.Announcements.Models.Entities;
using SmartSis.Api.Features.Announcements.Rules;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Announcements.Repositories;

public sealed class AnnouncementRepository(SisDbContext db) : IAnnouncementRepository
{
    public Task<PagedResult<AnnouncementDto>> FeedAsync(AnnouncementViewer viewer, DateTime now, PageQuery page, CancellationToken ct)
    {
        var me = viewer.UserId;
        var all = db.Set<Announcement>().AsNoTracking();
        IQueryable<Announcement> q;
        switch (viewer.Role)
        {
            case UserRole.Student:
                var majorId = viewer.MajorId;
                var mySections = db.Set<Enrollment>()
                    .Where(e => e.StudentId == me && e.Status == EnrollmentStatus.Enrolled)
                    .Select(e => (int?)e.SectionId);
                q = all.Where(a => a.PublishAt <= now && (a.ExpiresAt == null || a.ExpiresAt > now)
                                   && (a.Scope == AnnouncementScope.All
                                       || a.Scope == AnnouncementScope.Students
                                       || (a.Scope == AnnouncementScope.Major && a.MajorId == majorId)
                                       || (a.Scope == AnnouncementScope.Section && mySections.Contains(a.SectionId))));
                break;
            case UserRole.Instructor:
                q = all.Where(a => (a.AuthorId == me && a.PublishAt > now)
                                   || (a.PublishAt <= now && (a.ExpiresAt == null || a.ExpiresAt > now)
                                       && (a.Scope == AnnouncementScope.All
                                           || a.Scope == AnnouncementScope.Instructors
                                           || (a.Scope == AnnouncementScope.Section && a.Section!.InstructorId == me))));
                break;
            default:
                q = all.Where(a => (a.AuthorId == me && a.PublishAt > now)
                                   || (a.PublishAt <= now && (a.ExpiresAt == null || a.ExpiresAt > now)));
                break;
        }
        return PageAsync(q, viewer, now, page, ct);
    }

    public Task<PagedResult<AnnouncementDto>> ListAsync(
        AnnouncementViewer viewer, int? authorId, AnnouncementStatus? status, DateTime now, PageQuery page, CancellationToken ct)
    {
        var q = db.Set<Announcement>().AsNoTracking();
        if (authorId is { } author) q = q.Where(a => a.AuthorId == author);
        q = status switch
        {
            AnnouncementStatus.Scheduled => q.Where(a => a.PublishAt > now),
            AnnouncementStatus.Live => q.Where(a => a.PublishAt <= now && (a.ExpiresAt == null || a.ExpiresAt > now)),
            AnnouncementStatus.Expired => q.Where(a => a.PublishAt <= now && a.ExpiresAt != null && a.ExpiresAt <= now),
            _ => q,
        };
        return PageAsync(q, viewer, now, page, ct);
    }

    public async Task<AnnouncementDto?> GetDtoAsync(int id, AnnouncementViewer viewer, DateTime now, CancellationToken ct)
    {
        var row = await Project(db.Set<Announcement>().AsNoTracking().Where(a => a.Id == id)).FirstOrDefaultAsync(ct);
        return row?.ToDto(viewer, now);
    }

    public Task<Announcement?> FindAsync(int id, CancellationToken ct) =>
        db.Set<Announcement>().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<int?> GetStudentMajorIdAsync(int userId, CancellationToken ct) =>
        db.Set<Student>().Where(s => s.UserId == userId).Select(s => (int?)s.MajorId).FirstOrDefaultAsync(ct);

    public Task<bool> MajorExistsAsync(int majorId, CancellationToken ct) => db.Set<Major>().AnyAsync(m => m.Id == majorId, ct);

    public Task<AnnouncementSectionInfo?> GetSectionAsync(int sectionId, CancellationToken ct) =>
        db.Set<Section>().AsNoTracking().Where(s => s.Id == sectionId)
            .Select(s => new AnnouncementSectionInfo(s.Id, s.InstructorId, s.Course.Code + " " + s.SectionCode))
            .FirstOrDefaultAsync(ct);

    public Task<List<AnnouncementSectionDto>> ListCurrentSectionsTaughtByAsync(int instructorId, CancellationToken ct)
    {
        var enrollments = db.Set<Enrollment>();
        return db.Set<Section>().AsNoTracking()
            .Where(s => s.InstructorId == instructorId && s.Term.IsCurrent)
            .OrderBy(s => s.Course.Code).ThenBy(s => s.SectionCode)
            .Select(s => new AnnouncementSectionDto(
                s.Id, s.Course.Code + " " + s.SectionCode, s.Course.Name, s.Term.Name,
                enrollments.Count(e => e.SectionId == s.Id && e.Status == EnrollmentStatus.Enrolled)))
            .ToListAsync(ct);
    }

    public async Task<List<(int UserId, UserRole Role)>> GetAudienceAsync(Announcement a, CancellationToken ct)
    {
        // The User query filter already hides deactivated (soft-deleted) users.
        var users = db.Set<User>().AsNoTracking().Where(u => u.IsActive && u.Id != a.AuthorId);
        users = a.Scope switch
        {
            AnnouncementScope.All => users,
            AnnouncementScope.Students => users.Where(u => u.Role == UserRole.Student),
            AnnouncementScope.Instructors => users.Where(u => u.Role == UserRole.Instructor),
            AnnouncementScope.Major => users.Where(u => u.Role == UserRole.Student
                                                        && db.Set<Student>().Any(s => s.UserId == u.Id && s.MajorId == a.MajorId)),
            AnnouncementScope.Section => users.Where(u => db.Set<Enrollment>().Any(e =>
                e.StudentId == u.Id && e.SectionId == a.SectionId && e.Status == EnrollmentStatus.Enrolled)),
            _ => users.Where(_ => false),
        };
        var rows = await users.Select(u => new { u.Id, u.Role }).ToListAsync(ct);
        return rows.Select(r => (r.Id, r.Role)).ToList();
    }

    public async Task<bool> IsNotifiedAsync(int announcementId, CancellationToken ct) =>
        (await NotifiedIdsAsync([announcementId], ct)).Count > 0;

    public async Task<List<Announcement>> ListDueForNotificationAsync(DateTime since, DateTime now, CancellationToken ct)
    {
        var live = await db.Set<Announcement>().AsNoTracking()
            .Where(a => a.PublishAt > since && a.PublishAt <= now && (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderBy(a => a.PublishAt)
            .ToListAsync(ct);
        if (live.Count == 0) return live;

        // One query for all candidates (was one query per announcement).
        var notified = await NotifiedIdsAsync([.. live.Select(a => a.Id)], ct);
        return [.. live.Where(a => !notified.Contains(a.Id))];
    }

    /// <summary>
    /// Ids among <paramref name="ids"/> that already have announcement notifications. Matches the exact links
    /// <see cref="AnnouncementSchedule.LinkFor"/> builds (one per role), so SQL Server can seek the
    /// (Type, Link) index instead of scanning every notification with <c>LIKE '%marker'</c>.
    /// </summary>
    private async Task<HashSet<int>> NotifiedIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var byLink = new Dictionary<string, int>();
        foreach (var id in ids)
            foreach (var role in Enum.GetValues<UserRole>())
                byLink[AnnouncementSchedule.LinkFor(role, id)] = id;
        var links = byLink.Keys.ToList();

        var found = await db.Set<Notification>().AsNoTracking()
            .Where(n => n.Type == NotificationType.Announcement && n.Link != null && links.Contains(n.Link))
            .Select(n => n.Link!)
            .Distinct()
            .ToListAsync(ct);
        return [.. found.Select(l => byLink[l])];
    }

    public void Add(Announcement announcement) => db.Set<Announcement>().Add(announcement);
    public void Remove(Announcement announcement) => db.Set<Announcement>().Remove(announcement);

    private static async Task<PagedResult<AnnouncementDto>> PageAsync(
        IQueryable<Announcement> q, AnnouncementViewer viewer, DateTime now, PageQuery page, CancellationToken ct)
    {
        var total = await q.CountAsync(ct);
        var rows = await Project(q.OrderByDescending(a => a.PublishAt).ThenByDescending(a => a.Id).Skip(page.Skip).Take(page.Take))
            .ToListAsync(ct);
        return new PagedResult<AnnouncementDto>(rows.Select(r => r.ToDto(viewer, now)).ToList(), Math.Max(page.Page, 1), page.Take, total);
    }

    private static IQueryable<Row> Project(IQueryable<Announcement> q) =>
        q.Select(a => new Row(
            a.Id, a.Title, a.Body, a.Scope, a.MajorId, a.Major != null ? a.Major.Name : null,
            a.SectionId, a.Section != null ? a.Section.Course.Code + " " + a.Section.SectionCode : null,
            a.AuthorId, a.Author.FullName, a.Author.Role, a.PublishAt, a.ExpiresAt));

    /// <summary>Status and CanManage depend on the time and the viewer, so they are worked out after the query.</summary>
    private sealed record Row(
        int Id, string Title, string Body, AnnouncementScope Scope, int? MajorId, string? MajorName, int? SectionId, string? SectionLabel,
        int AuthorId, string AuthorName, UserRole AuthorRole, DateTime PublishAt, DateTime? ExpiresAt)
    {
        public AnnouncementDto ToDto(AnnouncementViewer viewer, DateTime now) => new(
            Id, Title, Body, Scope, MajorId, MajorName, SectionId, SectionLabel, AuthorName, AuthorRole, PublishAt, ExpiresAt,
            AnnouncementSchedule.StatusAt(PublishAt, ExpiresAt, now),
            viewer.Role == UserRole.Admin || AuthorId == viewer.UserId);
    }
}
