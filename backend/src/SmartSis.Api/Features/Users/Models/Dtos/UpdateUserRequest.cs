namespace SmartSis.Api.Features.Users.Models.Dtos;

/// <summary>Edit an account. University ID, role and password can't be changed here. Send the profile block that matches the user's role.</summary>
/// <param name="Email">Email (max 256). Stored trimmed and lower case. Unique, including deactivated accounts.</param>
/// <param name="FullName">Full name (max 150).</param>
/// <param name="Phone">Optional phone (max 32).</param>
/// <param name="DateOfBirth">Optional date of birth; must be in the past.</param>
/// <param name="Student">Required for students, otherwise null.</param>
/// <param name="Instructor">Required for instructors, otherwise null.</param>
public sealed record UpdateUserRequest(
    string Email,
    string FullName,
    string? Phone,
    DateOnly? DateOfBirth,
    StudentInfoRequest? Student,
    InstructorInfoRequest? Instructor);
