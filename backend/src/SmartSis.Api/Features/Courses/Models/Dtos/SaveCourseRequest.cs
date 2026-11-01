using SmartSis.Api.Features.Courses.Enums;

namespace SmartSis.Api.Features.Courses.Models.Dtos;

/// <summary>Create or update a course (same body for POST and PUT). PUT replaces both lists with what is sent.</summary>
/// <param name="Code">Course code, e.g. "CSC 101" or "csc101" (stored as "CSC 101"). 2–5 letters, optional space, 3 digits, optional letter.</param>
/// <param name="Name">Course name (max 150).</param>
/// <param name="Credits">Credits, 0–12.</param>
/// <param name="Description">Optional description (max 4000).</param>
/// <param name="DepartmentId">Owning department.</param>
/// <param name="PrerequisiteIds">IDs of courses that must be passed first. Required: send [] for none. No duplicates, no cycles.</param>
/// <param name="Majors">Majors this course belongs to, as Major or Elective. Required: send [] for none. One entry per major.</param>
public sealed record SaveCourseRequest(
    string Code,
    string Name,
    int Credits,
    string? Description,
    int DepartmentId,
    int[] PrerequisiteIds,
    CourseMajorRequest[] Majors);

/// <summary>How the course counts for one major.</summary>
/// <param name="MajorId">Major id.</param>
/// <param name="Type">"Major" or "Elective".</param>
public sealed record CourseMajorRequest(int MajorId, CourseType Type);
