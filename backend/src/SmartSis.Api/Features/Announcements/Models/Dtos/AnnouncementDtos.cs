using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Api.Features.Announcements.Models.Dtos;

/// <summary>An announcement as the signed-in user sees it.</summary>
/// <param name="Id">Announcement id.</param>
/// <param name="Title">Title, up to 200 characters.</param>
/// <param name="Body">Text, up to 4000 characters. Plain text; line breaks are kept.</param>
/// <param name="Scope">Audience: "All", "Students", "Instructors", "Major" or "Section".</param>
/// <param name="MajorId">Major, when Scope = Major.</param>
/// <param name="MajorName">Major name, when Scope = Major.</param>
/// <param name="SectionId">Section, when Scope = Section.</param>
/// <param name="SectionLabel">Section, e.g. "CSC 210 A", when Scope = Section.</param>
/// <param name="AuthorName">Who posted it.</param>
/// <param name="AuthorRole">"Admin" or "Instructor".</param>
/// <param name="PublishAt">When it appears in the feed (UTC).</param>
/// <param name="ExpiresAt">When it leaves the feed (UTC), or null to stay.</param>
/// <param name="Status">"Scheduled" (not published yet), "Live" or "Expired", at the time of the request.</param>
/// <param name="CanManage">True when the signed-in user may edit and delete it (admin: all; instructor: own).</param>
public sealed record AnnouncementDto(
    int Id,
    string Title,
    string Body,
    AnnouncementScope Scope,
    int? MajorId,
    string? MajorName,
    int? SectionId,
    string? SectionLabel,
    string AuthorName,
    UserRole AuthorRole,
    DateTime PublishAt,
    DateTime? ExpiresAt,
    AnnouncementStatus Status,
    bool CanManage);

/// <summary>Admin: post or edit an announcement for everyone, students, instructors or one major (rule C1).</summary>
/// <param name="Title">Up to 200 characters.</param>
/// <param name="Body">Up to 4000 characters.</param>
/// <param name="Scope">"All", "Students", "Instructors" or "Major". "Section" is only kept when editing an instructor's section post.</param>
/// <param name="MajorId">Required when Scope = Major, otherwise empty.</param>
/// <param name="SectionId">Only when editing a Section post: must stay the same section.</param>
/// <param name="PublishAt">UTC. Empty or in the past = publish now. In the future = scheduled.</param>
/// <param name="ExpiresAt">UTC, after PublishAt and in the future. Empty = never expires.</param>
public sealed record SaveAnnouncementRequest(
    string Title, string Body, AnnouncementScope Scope, int? MajorId, int? SectionId, DateTime? PublishAt, DateTime? ExpiresAt);

/// <summary>Instructor: post or edit an announcement for one section they teach (rule C1).</summary>
/// <param name="Title">Up to 200 characters.</param>
/// <param name="Body">Up to 4000 characters.</param>
/// <param name="SectionId">A section the instructor teaches.</param>
/// <param name="PublishAt">UTC. Empty or in the past = publish now. In the future = scheduled.</param>
/// <param name="ExpiresAt">UTC, after PublishAt and in the future. Empty = never expires.</param>
public sealed record SaveSectionAnnouncementRequest(string Title, string Body, int SectionId, DateTime? PublishAt, DateTime? ExpiresAt);

/// <summary>Page through announcements, newest publish date first.</summary>
/// <param name="Page">1-based page.</param>
/// <param name="PageSize">1–100, default 20.</param>
/// <param name="Status">Only "Scheduled", "Live" or "Expired" ones (manage lists only).</param>
public sealed record AnnouncementListQuery(int Page = 1, int PageSize = 20, AnnouncementStatus? Status = null);

/// <summary>A section the instructor can post to.</summary>
/// <param name="Id">Section id.</param>
/// <param name="Label">e.g. "CSC 210 A".</param>
/// <param name="CourseName">Course name.</param>
/// <param name="TermName">Term name.</param>
/// <param name="EnrolledCount">Students enrolled now (who will be notified).</param>
public sealed record AnnouncementSectionDto(int Id, string Label, string CourseName, string TermName, int EnrolledCount);
