using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Announcements.Interfaces;
using SmartSis.Api.Features.Announcements.Models.Dtos;
using SmartSis.Api.Features.Announcements.Models.Entities;
using SmartSis.Api.Features.Announcements.Rules;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Announcements.Services;

/// <summary>
/// PB-12 / rule C1. Admin posts to All / Students / Instructors / a Major; an instructor posts only to sections they teach.
/// Publishing creates one in-app notification per audience member: at once when the post is live, or later through
/// AnnouncementPublishJob when it is scheduled. Deleting removes the row (Announcements has no DeletedAt column).
/// </summary>
public sealed class AnnouncementService(
    IAnnouncementRepository repo,
    INotificationService notifications,
    ICurrentUser me,
    IUnitOfWork uow,
    TimeProvider clock) : IAnnouncementService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<PagedResult<AnnouncementDto>> FeedAsync(AnnouncementListQuery query, CancellationToken ct) =>
        await repo.FeedAsync(await ViewerAsync(ct), Now, Page(query), ct);

    public async Task<PagedResult<AnnouncementDto>> ListAllAsync(AnnouncementListQuery query, CancellationToken ct) =>
        await repo.ListAsync(await ViewerAsync(ct), authorId: null, query.Status, Now, Page(query), ct);

    public async Task<PagedResult<AnnouncementDto>> ListMineAsync(AnnouncementListQuery query, CancellationToken ct) =>
        await repo.ListAsync(await ViewerAsync(ct), me.UserId, query.Status, Now, Page(query), ct);

    public Task<List<AnnouncementSectionDto>> MySectionsAsync(CancellationToken ct) =>
        repo.ListCurrentSectionsTaughtByAsync(me.UserId, ct);

    public async Task<AnnouncementDto> CreateAsync(SaveAnnouncementRequest request, CancellationToken ct)
    {
        if (!AnnouncementSchedule.IsAdminScope(request.Scope))
            throw Invalid("scope", "Choose everyone, students, instructors or a major.");
        await EnsureMajorExistsAsync(request, ct);

        var announcement = new Announcement { AuthorId = me.UserId };
        Apply(announcement, request.Title, request.Body, request.PublishAt, request.ExpiresAt);
        announcement.Scope = request.Scope;
        announcement.MajorId = request.MajorId;
        return await SaveNewAsync(announcement, ct);
    }

    public async Task<AnnouncementDto> UpdateAsync(int id, SaveAnnouncementRequest request, CancellationToken ct)
    {
        var announcement = await repo.FindAsync(id, ct) ?? throw NotFoundException.For<Announcement>(id);

        // An admin keeps an instructor's section post on its section; otherwise only the admin scopes (rule C1).
        var keepsSection = request.Scope == AnnouncementScope.Section
                           && announcement.Scope == AnnouncementScope.Section
                           && request.SectionId == announcement.SectionId;
        if (!AnnouncementSchedule.IsAdminScope(request.Scope) && !keepsSection)
            throw Invalid("scope", "Choose everyone, students, instructors or a major.");
        await EnsureMajorExistsAsync(request, ct);

        Apply(announcement, request.Title, request.Body, request.PublishAt, request.ExpiresAt);
        announcement.Scope = request.Scope;
        announcement.MajorId = request.MajorId;
        announcement.SectionId = keepsSection ? announcement.SectionId : null;
        return await SaveChangesAsync(announcement, ct);
    }

    public async Task<AnnouncementDto> CreateForSectionAsync(SaveSectionAnnouncementRequest request, CancellationToken ct)
    {
        await EnsureTeachesAsync(request.SectionId, ct);
        var announcement = new Announcement { AuthorId = me.UserId, Scope = AnnouncementScope.Section, SectionId = request.SectionId };
        Apply(announcement, request.Title, request.Body, request.PublishAt, request.ExpiresAt);
        return await SaveNewAsync(announcement, ct);
    }

    public async Task<AnnouncementDto> UpdateForSectionAsync(int id, SaveSectionAnnouncementRequest request, CancellationToken ct)
    {
        var announcement = await repo.FindAsync(id, ct) ?? throw NotFoundException.For<Announcement>(id);
        if (announcement.AuthorId != me.UserId)
            throw new ForbiddenException("You can only edit announcements you posted.");
        await EnsureTeachesAsync(request.SectionId, ct);

        Apply(announcement, request.Title, request.Body, request.PublishAt, request.ExpiresAt);
        announcement.Scope = AnnouncementScope.Section;
        announcement.SectionId = request.SectionId;
        announcement.MajorId = null;
        return await SaveChangesAsync(announcement, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var announcement = await repo.FindAsync(id, ct) ?? throw NotFoundException.For<Announcement>(id);
        if (!me.IsInRole(Roles.Admin) && announcement.AuthorId != me.UserId)
            throw new ForbiddenException("You can only delete announcements you posted.");
        repo.Remove(announcement);
        await uow.SaveChangesAsync(ct);
    }

    public async Task<int> NotifyDueAsync(TimeSpan lookback, CancellationToken ct)
    {
        var now = Now;
        var due = await repo.ListDueForNotificationAsync(now - lookback, now, ct);
        foreach (var a in due) await NotifyAudienceAsync(a, ct);
        if (due.Count > 0) await uow.SaveChangesAsync(ct);
        return due.Count;
    }

    private async Task<AnnouncementDto> SaveNewAsync(Announcement announcement, CancellationToken ct)
    {
        await uow.ExecuteInTransactionAsync(async () =>
        {
            repo.Add(announcement);
            await uow.SaveChangesAsync(ct);   // the id goes into the notification links
            await NotifyIfLiveAsync(announcement, ct);
            await uow.SaveChangesAsync(ct);
        }, ct);
        return await GetDtoAsync(announcement.Id, ct);
    }

    private async Task<AnnouncementDto> SaveChangesAsync(Announcement announcement, CancellationToken ct)
    {
        await uow.ExecuteInTransactionAsync(async () =>
        {
            // A scheduled post published now by this edit gets its notifications; one already notified doesn't again.
            if (!await repo.IsNotifiedAsync(announcement.Id, ct)) await NotifyIfLiveAsync(announcement, ct);
            await uow.SaveChangesAsync(ct);
        }, ct);
        return await GetDtoAsync(announcement.Id, ct);
    }

    private async Task NotifyIfLiveAsync(Announcement a, CancellationToken ct)
    {
        if (AnnouncementSchedule.StatusAt(a.PublishAt, a.ExpiresAt, Now) == AnnouncementStatus.Live)
            await NotifyAudienceAsync(a, ct);
    }

    private async Task NotifyAudienceAsync(Announcement a, CancellationToken ct)
    {
        var audience = await repo.GetAudienceAsync(a, ct);
        if (audience.Count == 0) return;
        notifications.NotifyMany(
            audience.Select(u => (u.UserId, (string?)AnnouncementSchedule.LinkFor(u.Role, a.Id))),
            NotificationType.Announcement,
            a.Title,
            a.Body);
    }

    private void Apply(Announcement a, string title, string body, DateTime? publishAt, DateTime? expiresAt)
    {
        var now = Now;
        var current = a.Id == 0 ? (DateTime?)null : a.PublishAt;
        var publish = AnnouncementSchedule.ResolvePublishAt(publishAt, current, now);
        if (AnnouncementSchedule.CheckExpiry(publish, expiresAt, now) is { } error)
            throw Invalid("expiresAt", error);

        a.Title = title.Trim();
        a.Body = body.Trim();
        a.PublishAt = publish;
        a.ExpiresAt = expiresAt;
    }

    private async Task EnsureMajorExistsAsync(SaveAnnouncementRequest r, CancellationToken ct)
    {
        if (r.MajorId is { } majorId && !await repo.MajorExistsAsync(majorId, ct))
            throw Invalid("majorId", $"Major {majorId} doesn't exist.");
    }

    /// <summary>Rule C1: an instructor posts only to sections they teach.</summary>
    private async Task EnsureTeachesAsync(int sectionId, CancellationToken ct)
    {
        var section = await repo.GetSectionAsync(sectionId, ct) ?? throw Invalid("sectionId", $"Section {sectionId} doesn't exist.");
        if (section.InstructorId != me.UserId)
            throw new ForbiddenException($"You don't teach {section.Label}, so you can't post to it.");
    }

    private async Task<AnnouncementDto> GetDtoAsync(int id, CancellationToken ct) =>
        await repo.GetDtoAsync(id, await ViewerAsync(ct), Now, ct) ?? throw NotFoundException.For<Announcement>(id);

    private async Task<AnnouncementViewer> ViewerAsync(CancellationToken ct)
    {
        var role = Enum.Parse<UserRole>(me.Role);
        var majorId = role == UserRole.Student ? await repo.GetStudentMajorIdAsync(me.UserId, ct) : null;
        return new AnnouncementViewer(me.UserId, role, majorId);
    }

    private static PageQuery Page(AnnouncementListQuery q) => new(q.Page, q.PageSize);

    private static ValidationException Invalid(string field, string message) => new([new ValidationFailure(field, message)]);
}
