namespace SmartSis.Api.Features.Users.Models.Dtos;

/// <summary>Student profile fields.</summary>
/// <param name="MajorId">Major id.</param>
/// <param name="CampusId">Campus id.</param>
/// <param name="EnrollmentDate">Date the student joined the university.</param>
/// <param name="MaxCreditsOverride">Credit cap per term for this student, 1–30. Null = the major's default.</param>
public sealed record StudentInfoRequest(int MajorId, int CampusId, DateOnly EnrollmentDate, int? MaxCreditsOverride);
