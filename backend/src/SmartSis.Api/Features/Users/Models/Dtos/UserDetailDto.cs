using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Api.Features.Users.Models.Dtos;

/// <summary>An account as the admin sees it (also for deactivated accounts). Never contains the password hash.</summary>
/// <param name="Id">User id.</param>
/// <param name="UniversityId">Numeric login ID.</param>
/// <param name="FullName">Full name.</param>
/// <param name="Email">Email.</param>
/// <param name="Role">"Student", "Instructor" or "Admin".</param>
/// <param name="Phone">Phone, if any.</param>
/// <param name="DateOfBirth">Date of birth, if any.</param>
/// <param name="PhotoUrl">Photo URL, if any.</param>
/// <param name="IsActive">False once deactivated.</param>
/// <param name="DeletedAt">When the account was deactivated (UTC), or null.</param>
/// <param name="Student">Student profile, for students.</param>
/// <param name="Instructor">Instructor profile, for instructors.</param>
public sealed record UserDetailDto(
    int Id,
    long UniversityId,
    string FullName,
    string Email,
    UserRole Role,
    string? Phone,
    DateOnly? DateOfBirth,
    string? PhotoUrl,
    bool IsActive,
    DateTime? DeletedAt,
    UserStudentDto? Student,
    UserInstructorDto? Instructor);

/// <summary>Student profile.</summary>
/// <param name="MajorId">Major id.</param>
/// <param name="MajorName">Major name.</param>
/// <param name="CampusId">Campus id.</param>
/// <param name="CampusName">Campus name.</param>
/// <param name="EnrollmentDate">Date the student joined.</param>
/// <param name="MaxCreditsOverride">Admin override of the credit cap, or null.</param>
/// <param name="EffectiveMaxCredits">Credit cap that applies: the override, else the major's default (rule E2).</param>
public sealed record UserStudentDto(
    int MajorId,
    string MajorName,
    int CampusId,
    string CampusName,
    DateOnly EnrollmentDate,
    int? MaxCreditsOverride,
    int EffectiveMaxCredits);

/// <summary>Instructor profile.</summary>
/// <param name="DepartmentId">Department id.</param>
/// <param name="DepartmentName">Department name.</param>
public sealed record UserInstructorDto(int DepartmentId, string DepartmentName);
