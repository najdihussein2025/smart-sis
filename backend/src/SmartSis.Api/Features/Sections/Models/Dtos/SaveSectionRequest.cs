using SmartSis.Api.Features.Sections.Enums;

namespace SmartSis.Api.Features.Sections.Models.Dtos;

/// <summary>Create or update a section (same body for POST and PUT). The meeting list replaces the stored one.</summary>
/// <param name="CourseId">Course id.</param>
/// <param name="TermId">Term id.</param>
/// <param name="SectionCode">1–8 letters or digits, stored upper case ("a" → "A"). Unique per course and term.</param>
/// <param name="InstructorId">Instructor user id (from <c>/api/admin/lookups</c>), or null to leave unassigned.</param>
/// <param name="Capacity">Maximum seats, 1–500. Can't go below the seats already taken.</param>
/// <param name="Meetings">At least one weekly meeting, at most 10.</param>
public sealed record SaveSectionRequest(
    int CourseId,
    int TermId,
    string SectionCode,
    int? InstructorId,
    int Capacity,
    SaveMeetingRequest[] Meetings);

/// <summary>A weekly meeting.</summary>
/// <param name="Id">Existing meeting id to keep it on update; null for a new meeting.</param>
/// <param name="DayOfWeek">1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">Start, e.g. "08:00:00".</param>
/// <param name="EndTime">End, after <paramref name="StartTime"/>.</param>
/// <param name="RoomId">Room id (from <c>/api/admin/lookups</c>).</param>
/// <param name="Type">"Lecture" or "Lab".</param>
public sealed record SaveMeetingRequest(int? Id, int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int RoomId, MeetingType Type);
