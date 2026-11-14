using SmartSis.Api.Features.Sections.Enums;

namespace SmartSis.Api.Features.Schedule.Models.Dtos;

/// <summary>A weekly timetable for the current term.</summary>
/// <param name="TermId">Current term id, or null when no term is current (no items then).</param>
/// <param name="TermName">Current term name.</param>
/// <param name="Items">One row per weekly meeting, by day then start time.</param>
public sealed record ScheduleDto(int? TermId, string? TermName, IReadOnlyList<ScheduleItemDto> Items);

/// <summary>One weekly class.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="MeetingId">Meeting id.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="CourseName">Course name.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="DayOfWeek">1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">Start, e.g. "08:00:00".</param>
/// <param name="EndTime">End.</param>
/// <param name="RoomName">Room.</param>
/// <param name="InstructorName">Instructor, or null when not assigned.</param>
/// <param name="Type">"Lecture" or "Lab".</param>
public sealed record ScheduleItemDto(
    int SectionId,
    int MeetingId,
    string CourseCode,
    string CourseName,
    string SectionCode,
    int DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string RoomName,
    string? InstructorName,
    MeetingType Type);
