using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Api.Features.Users.Models.Dtos;

/// <summary>Which accounts to list by activation state.</summary>
public enum UserStatusFilter
{
    Active,
    Inactive,
    All,
}

/// <summary>Filters and paging for the admin user list.</summary>
/// <param name="Search">Matches full name, email or University ID (contains, case-insensitive).</param>
/// <param name="Role">Only this role.</param>
/// <param name="Status">Active (default), Inactive or All.</param>
/// <param name="MajorId">Only students of this major.</param>
/// <param name="DepartmentId">Instructors of this department and students whose major belongs to it.</param>
/// <param name="Page">1-based page number (default 1).</param>
/// <param name="PageSize">Rows per page, 1–100 (default 20).</param>
public sealed record UserSearchQuery(
    [property: FromQuery(Name = "search")] string? Search = null,
    [property: FromQuery(Name = "role")] UserRole? Role = null,
    [property: FromQuery(Name = "status")] UserStatusFilter Status = UserStatusFilter.Active,
    [property: FromQuery(Name = "majorId")] int? MajorId = null,
    [property: FromQuery(Name = "departmentId")] int? DepartmentId = null,
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);
