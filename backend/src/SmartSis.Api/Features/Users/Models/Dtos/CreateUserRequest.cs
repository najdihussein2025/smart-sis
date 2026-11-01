using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Api.Features.Users.Models.Dtos;

/// <summary>Create a student or instructor account. Send exactly the profile block that matches <paramref name="Role"/>.</summary>
/// <param name="UniversityId">Numeric login ID, 4–9 digits (1000–999999999). Unique, including deactivated accounts. Can't be changed later.</param>
/// <param name="Email">Email (max 256). Stored trimmed and lower case. Unique, including deactivated accounts.</param>
/// <param name="FullName">Full name (max 150).</param>
/// <param name="Phone">Optional phone (max 32).</param>
/// <param name="DateOfBirth">Optional date of birth; must be in the past.</param>
/// <param name="Role">"Student" or "Instructor". Admin accounts can't be created here. Can't be changed later.</param>
/// <param name="InitialPassword">Initial password: at least 8 characters with a letter and a number (same rule as reset password). Hashed, never returned.</param>
/// <param name="Student">Required when Role is Student, otherwise null.</param>
/// <param name="Instructor">Required when Role is Instructor, otherwise null.</param>
public sealed record CreateUserRequest(
    long UniversityId,
    string Email,
    string FullName,
    string? Phone,
    DateOnly? DateOfBirth,
    UserRole Role,
    string InitialPassword,
    StudentInfoRequest? Student,
    InstructorInfoRequest? Instructor);
