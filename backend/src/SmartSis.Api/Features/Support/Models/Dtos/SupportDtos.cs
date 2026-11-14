using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Api.Features.Support.Models.Dtos;

/// <summary>A support category students choose from.</summary>
/// <param name="Id">Category id.</param>
/// <param name="Name">e.g. "Registration", "Grade dispute".</param>
public sealed record SupportCategoryDto(int Id, string Name);

/// <summary>A support category with how many requests use it (admin).</summary>
/// <param name="Id">Category id.</param>
/// <param name="Name">Name, unique.</param>
/// <param name="RequestCount">Requests filed under it. A category in use can't be deleted.</param>
public sealed record AdminSupportCategoryDto(int Id, string Name, int RequestCount);

/// <summary>Add or rename a support category.</summary>
/// <param name="Name">1–100 characters, unique (case-insensitive).</param>
public sealed record SaveSupportCategoryRequest(string Name);

/// <summary>Submit a support request.</summary>
/// <param name="CategoryId">An existing support category.</param>
/// <param name="Subject">Short title, up to 200 characters.</param>
/// <param name="Description">What happened, up to 4000 characters.</param>
public sealed record CreateSupportRequestRequest(int CategoryId, string Subject, string Description);

/// <summary>A reply in a request's thread.</summary>
/// <param name="Body">The message, up to 4000 characters.</param>
public sealed record SupportReplyRequest(string Body);

/// <summary>Admin: move a request to another status.</summary>
/// <param name="Status">"Open", "InProgress", "Resolved" or "Closed".</param>
public sealed record ChangeSupportStatusRequest(SupportStatus Status);

/// <summary>Admin: assign a request.</summary>
/// <param name="AdminId">User id of an active admin, or null to unassign.</param>
public sealed record AssignSupportRequestRequest(int? AdminId);

/// <summary>A row in the student's list of requests.</summary>
/// <param name="Id">Request id.</param>
/// <param name="CategoryId">Category id.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="Subject">Subject.</param>
/// <param name="Status">"Open", "InProgress", "Resolved" or "Closed".</param>
/// <param name="CreatedAt">Submitted at (UTC).</param>
/// <param name="LastActivityAt">Newest reply, or submission time when there is none (UTC).</param>
/// <param name="MessageCount">Replies in the thread.</param>
public sealed record SupportRequestSummaryDto(
    int Id, int CategoryId, string CategoryName, string Subject, SupportStatus Status,
    DateTime CreatedAt, DateTime LastActivityAt, int MessageCount);

/// <summary>One reply in a thread, oldest first.</summary>
/// <param name="Id">Message id.</param>
/// <param name="AuthorId">User id of the author.</param>
/// <param name="AuthorName">Author's full name.</param>
/// <param name="AuthorRole">"Student" or "Admin".</param>
/// <param name="Body">Text.</param>
/// <param name="CreatedAt">Sent at (UTC).</param>
public sealed record SupportMessageDto(int Id, int AuthorId, string AuthorName, UserRole AuthorRole, string Body, DateTime CreatedAt);

/// <summary>A request with its whole thread (student view).</summary>
/// <param name="Id">Request id.</param>
/// <param name="CategoryId">Category id.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="Subject">Subject.</param>
/// <param name="Description">The student's description.</param>
/// <param name="Status">"Open", "InProgress", "Resolved" or "Closed".</param>
/// <param name="CreatedAt">Submitted at (UTC).</param>
/// <param name="ResolvedAt">Set when resolved or closed, cleared when reopened.</param>
/// <param name="Messages">Replies, oldest first.</param>
public sealed record SupportRequestDetailDto(
    int Id, int CategoryId, string CategoryName, string Subject, string Description, SupportStatus Status,
    DateTime CreatedAt, DateTime? ResolvedAt, IReadOnlyList<SupportMessageDto> Messages);

/// <summary>A row in the admin inbox.</summary>
/// <param name="Id">Request id.</param>
/// <param name="Subject">Subject.</param>
/// <param name="CategoryId">Category id.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="Status">"Open", "InProgress", "Resolved" or "Closed".</param>
/// <param name="StudentId">Student user id.</param>
/// <param name="StudentUniversityId">Student university id.</param>
/// <param name="StudentName">Student full name.</param>
/// <param name="AssignedAdminId">Admin user id handling it, or null.</param>
/// <param name="AssignedAdminName">That admin's name, or null.</param>
/// <param name="CreatedAt">Submitted at (UTC).</param>
/// <param name="LastActivityAt">Newest reply, or submission time (UTC).</param>
/// <param name="MessageCount">Replies in the thread.</param>
public sealed record AdminSupportRequestDto(
    int Id, string Subject, int CategoryId, string CategoryName, SupportStatus Status,
    int StudentId, long StudentUniversityId, string StudentName,
    int? AssignedAdminId, string? AssignedAdminName,
    DateTime CreatedAt, DateTime LastActivityAt, int MessageCount);

/// <summary>A request with its thread (admin view).</summary>
/// <param name="Id">Request id.</param>
/// <param name="Subject">Subject.</param>
/// <param name="Description">The student's description.</param>
/// <param name="CategoryId">Category id.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="Status">"Open", "InProgress", "Resolved" or "Closed".</param>
/// <param name="StudentId">Student user id.</param>
/// <param name="StudentUniversityId">Student university id.</param>
/// <param name="StudentName">Student full name.</param>
/// <param name="StudentEmail">Student email.</param>
/// <param name="AssignedAdminId">Admin user id handling it, or null.</param>
/// <param name="AssignedAdminName">That admin's name, or null.</param>
/// <param name="CreatedAt">Submitted at (UTC).</param>
/// <param name="ResolvedAt">Set when resolved or closed.</param>
/// <param name="Messages">Replies, oldest first.</param>
public sealed record AdminSupportRequestDetailDto(
    int Id, string Subject, string Description, int CategoryId, string CategoryName, SupportStatus Status,
    int StudentId, long StudentUniversityId, string StudentName, string StudentEmail,
    int? AssignedAdminId, string? AssignedAdminName,
    DateTime CreatedAt, DateTime? ResolvedAt, IReadOnlyList<SupportMessageDto> Messages);

/// <summary>An admin who can be assigned a request.</summary>
/// <param name="Id">User id.</param>
/// <param name="FullName">Full name.</param>
/// <param name="UniversityId">University id.</param>
public sealed record SupportAdminDto(int Id, string FullName, long UniversityId);

/// <summary>Filters and paging for the student's own requests. Newest activity first.</summary>
/// <param name="Status">Only this status.</param>
/// <param name="Page">1-based page (default 1).</param>
/// <param name="PageSize">Rows per page, 1–100 (default 20).</param>
public sealed record MySupportRequestsQuery(
    [property: FromQuery(Name = "status")] SupportStatus? Status = null,
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);

/// <summary>Which requests to show by assignee.</summary>
public enum SupportAssignment
{
    /// <summary>Every request.</summary>
    Any,

    /// <summary>Assigned to the signed-in admin.</summary>
    Me,

    /// <summary>Not assigned yet.</summary>
    Unassigned,
}

/// <summary>Filters and paging for the admin inbox. Newest activity first.</summary>
/// <param name="Status">Only this status.</param>
/// <param name="CategoryId">Only this category.</param>
/// <param name="Assigned">"Any" (default), "Me" or "Unassigned".</param>
/// <param name="Search">Matches subject, student name or university id (contains, case-insensitive).</param>
/// <param name="Page">1-based page (default 1).</param>
/// <param name="PageSize">Rows per page, 1–100 (default 20).</param>
public sealed record AdminSupportSearchQuery(
    [property: FromQuery(Name = "status")] SupportStatus? Status = null,
    [property: FromQuery(Name = "categoryId")] int? CategoryId = null,
    [property: FromQuery(Name = "assigned")] SupportAssignment Assigned = SupportAssignment.Any,
    [property: FromQuery(Name = "search")] string? Search = null,
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);
