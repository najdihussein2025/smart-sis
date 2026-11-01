using SmartSis.Api.Features.Courses.Enums;

namespace SmartSis.Api.Features.Courses.Models.Dtos;

/// <summary>A course with its prerequisites and major mapping.</summary>
/// <param name="Id">Course id.</param>
/// <param name="Code">Course code, e.g. "CSC 101".</param>
/// <param name="Name">Course name.</param>
/// <param name="Credits">Credits.</param>
/// <param name="Description">Description, if any.</param>
/// <param name="DepartmentId">Owning department id.</param>
/// <param name="DepartmentName">Owning department name.</param>
/// <param name="Prerequisites">Courses that must be passed first, ordered by code.</param>
/// <param name="Majors">Majors this course belongs to, ordered by major name.</param>
public sealed record CourseDetailDto(
    int Id,
    string Code,
    string Name,
    int Credits,
    string? Description,
    int DepartmentId,
    string DepartmentName,
    IReadOnlyList<CourseRefDto> Prerequisites,
    IReadOnlyList<CourseMajorDto> Majors);

/// <summary>A short reference to a course.</summary>
/// <param name="Id">Course id.</param>
/// <param name="Code">Course code.</param>
/// <param name="Name">Course name.</param>
public sealed record CourseRefDto(int Id, string Code, string Name);

/// <summary>How the course counts for one major.</summary>
/// <param name="MajorId">Major id.</param>
/// <param name="MajorName">Major name.</param>
/// <param name="Type">"Major" or "Elective".</param>
public sealed record CourseMajorDto(int MajorId, string MajorName, CourseType Type);
