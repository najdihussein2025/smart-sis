namespace SmartSis.Api.Features.Users.Models.Dtos;

public sealed record UserProfileDto(
    int Id,
    long UniversityId,
    string FullName,
    string Email,
    string Role,
    string? Phone,
    DateOnly? DateOfBirth,
    string? PhotoUrl,
    StudentProfileDto? Student,
    InstructorProfileDto? Instructor);

public sealed record StudentProfileDto(
    int MajorId,
    string MajorName,
    string CampusName,
    DateOnly EnrollmentDate,
    int MaxCredits);

public sealed record InstructorProfileDto(int DepartmentId, string DepartmentName);
