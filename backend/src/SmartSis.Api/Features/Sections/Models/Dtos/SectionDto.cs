using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Sections.Enums;

namespace SmartSis.Api.Features.Sections.Models.Dtos;

/// <summary>A section with its weekly meetings and seat count.</summary>
/// <param name="Id">Section id.</param>
/// <param name="CourseId">Course id.</param>
/// <param name="CourseCode">Course code, e.g. "CSC 101".</param>
/// <param name="CourseName">Course name.</param>
/// <param name="TermId">Term id.</param>
/// <param name="TermName">Term name, e.g. "Fall 2026".</param>
/// <param name="SectionCode">Section code, e.g. "A". Unique per course and term.</param>
/// <param name="InstructorId">Instructor user id, or null when none is assigned yet.</param>
/// <param name="InstructorName">Instructor full name, or null.</param>
/// <param name="Capacity">Maximum seats.</param>
/// <param name="SeatsTaken">Students enrolled (or completed) in this section. Counted, never stored.</param>
/// <param name="Meetings">Weekly meetings, by day then start time.</param>
public sealed record SectionDto(
    int Id,
    int CourseId,
    string CourseCode,
    string CourseName,
    int TermId,
    string TermName,
    string SectionCode,
    int? InstructorId,
    string? InstructorName,
    int Capacity,
    int SeatsTaken,
    IReadOnlyList<SectionMeetingDto> Meetings);

/// <summary>A weekly meeting. Times are local wall-clock times ("08:00:00").</summary>
/// <param name="Id">Meeting id. Send it back on update to keep the meeting (and its attendance history).</param>
/// <param name="DayOfWeek">1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">Start, e.g. "08:00:00".</param>
/// <param name="EndTime">End, after <paramref name="StartTime"/>.</param>
/// <param name="RoomId">Room id.</param>
/// <param name="RoomName">Room name, e.g. "B-101".</param>
/// <param name="Type">"Lecture" or "Lab".</param>
public sealed record SectionMeetingDto(int Id, int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int RoomId, string RoomName, MeetingType Type);

/// <summary>Filters and paging for the admin section list.</summary>
/// <param name="TermId">Only sections of this term.</param>
/// <param name="CourseId">Only sections of this course.</param>
/// <param name="Search">Matches course code or name (contains, case-insensitive).</param>
/// <param name="Page">1-based page number (default 1).</param>
/// <param name="PageSize">Rows per page, 1–100 (default 20).</param>
public sealed record SectionSearchQuery(
    [property: FromQuery(Name = "termId")] int? TermId = null,
    [property: FromQuery(Name = "courseId")] int? CourseId = null,
    [property: FromQuery(Name = "search")] string? Search = null,
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 20);
