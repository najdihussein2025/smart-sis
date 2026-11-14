using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Sections.Models.Dtos;

namespace SmartSis.Api.Features.Courses.Models.Dtos;

/// <summary>The current term's catalog for the signed-in student.</summary>
/// <param name="TermId">Current term id, or null when no term is current (the list is then empty).</param>
/// <param name="TermName">Current term name.</param>
/// <param name="Window">Registration window open right now: "Closed", "Enrollment" or "AddDrop".</param>
/// <param name="MajorId">The major the list is filtered by (the student's own major unless another was asked for).</param>
/// <param name="Courses">Courses with at least one section this term, by code.</param>
public sealed record CatalogDto(int? TermId, string? TermName, string Window, int? MajorId, IReadOnlyList<CatalogCourseDto> Courses);

/// <summary>A course offered this term.</summary>
/// <param name="Id">Course id.</param>
/// <param name="Code">Course code, e.g. "CSC 210".</param>
/// <param name="Name">Course name.</param>
/// <param name="Credits">Credits.</param>
/// <param name="DepartmentName">Department.</param>
/// <param name="Type">"Major" or "Elective" for the chosen major; null when the course isn't mapped to it.</param>
/// <param name="SectionCount">Sections offered this term.</param>
/// <param name="SeatsLeft">Free seats over all its sections.</param>
/// <param name="Status">The student's status: "Available", "InCart" or "Enrolled".</param>
public sealed record CatalogCourseDto(
    int Id, string Code, string Name, int Credits, string DepartmentName, CourseType? Type, int SectionCount, int SeatsLeft, string Status);

/// <summary>A course with its prerequisites and this term's sections.</summary>
/// <param name="Id">Course id.</param>
/// <param name="Code">Course code.</param>
/// <param name="Name">Course name.</param>
/// <param name="Credits">Credits.</param>
/// <param name="Description">Description, or null.</param>
/// <param name="DepartmentName">Department.</param>
/// <param name="Prerequisites">Courses to pass (D or better) first, with whether the student already passed them.</param>
/// <param name="Sections">This term's sections with day, time, room, instructor and seats.</param>
public sealed record CatalogCourseDetailDto(
    int Id,
    string Code,
    string Name,
    int Credits,
    string? Description,
    string DepartmentName,
    IReadOnlyList<CatalogPrerequisiteDto> Prerequisites,
    IReadOnlyList<CatalogSectionDto> Sections);

/// <summary>A prerequisite of a course.</summary>
/// <param name="Id">Course id.</param>
/// <param name="Code">Course code.</param>
/// <param name="Name">Course name.</param>
/// <param name="Passed">True when the student has a completed attempt with D or better (rule E3).</param>
public sealed record CatalogPrerequisiteDto(int Id, string Code, string Name, bool Passed);

/// <summary>A section as a student sees it.</summary>
/// <param name="Id">Section id.</param>
/// <param name="SectionCode">Section code, e.g. "A".</param>
/// <param name="InstructorName">Instructor, or null when not assigned yet.</param>
/// <param name="Capacity">Maximum seats.</param>
/// <param name="SeatsTaken">Seats taken (counted from enrollments).</param>
/// <param name="SeatsLeft">Capacity minus seats taken, never below 0.</param>
/// <param name="Meetings">Weekly meetings.</param>
/// <param name="Status">The student's status for this section: "Available", "InCart" or "Enrolled".</param>
public sealed record CatalogSectionDto(
    int Id,
    string SectionCode,
    string? InstructorName,
    int Capacity,
    int SeatsTaken,
    int SeatsLeft,
    IReadOnlyList<SectionMeetingDto> Meetings,
    string Status);

/// <summary>A major, for the catalog's major filter.</summary>
public sealed record CatalogMajorDto(int Id, string Name);

/// <summary>Catalog filters.</summary>
/// <param name="Type">"Major" or "Elective" tab. Empty = both.</param>
/// <param name="MajorId">Major to list courses for. Empty = the student's own major.</param>
/// <param name="Search">Matches code or name.</param>
public sealed record CatalogQuery(
    [property: FromQuery(Name = "type")] CourseType? Type = null,
    [property: FromQuery(Name = "majorId")] int? MajorId = null,
    [property: FromQuery(Name = "search")] string? Search = null);

/// <summary>Values of <c>Status</c> on catalog rows.</summary>
public static class CatalogStatus
{
    public const string Available = "Available";
    public const string InCart = "InCart";
    public const string Enrolled = "Enrolled";
}
