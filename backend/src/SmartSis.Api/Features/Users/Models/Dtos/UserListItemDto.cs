using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Api.Features.Users.Models.Dtos;

/// <summary>One row of the admin user list.</summary>
/// <param name="Id">User id (use it in /api/admin/users/{id}).</param>
/// <param name="UniversityId">Numeric login ID.</param>
/// <param name="FullName">Full name.</param>
/// <param name="Email">Email.</param>
/// <param name="Role">"Student", "Instructor" or "Admin".</param>
/// <param name="IsActive">False once deactivated.</param>
/// <param name="MajorName">Students: major name.</param>
/// <param name="DepartmentName">Instructors: department name.</param>
public sealed record UserListItemDto(
    int Id,
    long UniversityId,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive,
    string? MajorName,
    string? DepartmentName);
