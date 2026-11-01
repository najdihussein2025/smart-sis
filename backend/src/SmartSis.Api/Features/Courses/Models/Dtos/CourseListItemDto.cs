using Microsoft.AspNetCore.Mvc;

namespace SmartSis.Api.Features.Courses.Models.Dtos;

/// <summary>One row of the admin course list.</summary>
/// <param name="Id">Course id.</param>
/// <param name="Code">Course code, e.g. "CSC 101".</param>
/// <param name="Name">Course name.</param>
/// <param name="Credits">Credits.</param>
/// <param name="DepartmentCode">Department code, e.g. "CSC".</param>
/// <param name="DepartmentName">Department name.</param>
public sealed record CourseListItemDto(int Id, string Code, string Name, int Credits, string DepartmentCode, string DepartmentName);

/// <summary>Filters and paging for the admin course list.</summary>
/// <param name="Search">Matches code or name (contains, case-insensitive).</param>
/// <param name="DepartmentId">Only courses of this department.</param>
/// <param name="Page">1-based page number (default 1).</param>
/// <param name="PageSize">Rows per page, 1–100 (default 20).</param>
public sealed record CourseSearchQuery(
    [property: FromQuery(Name = "search")] string? Search = null,
    [property: FromQuery(Name = "departmentId")] int? DepartmentId = null,
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);
