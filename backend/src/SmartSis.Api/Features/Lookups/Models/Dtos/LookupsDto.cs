namespace SmartSis.Api.Features.Lookups.Models.Dtos;

/// <summary>Everything the admin forms need for their dropdowns, in one call. Each list is ordered for display.</summary>
/// <param name="Campuses">All campuses, by name.</param>
/// <param name="Departments">All departments, by code.</param>
/// <param name="Majors">All majors, by name.</param>
/// <param name="Rooms">All rooms, by campus then name.</param>
/// <param name="Instructors">Active instructors only, by name. <c>Id</c> is the value to send as <c>instructorId</c>.</param>
public sealed record LookupsDto(
    IReadOnlyList<CampusOptionDto> Campuses,
    IReadOnlyList<DepartmentOptionDto> Departments,
    IReadOnlyList<MajorOptionDto> Majors,
    IReadOnlyList<RoomOptionDto> Rooms,
    IReadOnlyList<InstructorOptionDto> Instructors);

/// <summary>A campus.</summary>
public sealed record CampusOptionDto(int Id, string Name);

/// <summary>A department, e.g. CSC / Computer Science.</summary>
public sealed record DepartmentOptionDto(int Id, string Code, string Name);

/// <summary>A major with its default credit cap per term.</summary>
public sealed record MajorOptionDto(int Id, string Name, int DepartmentId, int DefaultMaxCredits);

/// <summary>A room and the campus it is on.</summary>
public sealed record RoomOptionDto(int Id, string Name, int CampusId, string CampusName, int Capacity);

/// <summary>An active instructor. <c>Id</c> is the user id (Instructors.UserId).</summary>
public sealed record InstructorOptionDto(int Id, string FullName, long UniversityId, int DepartmentId);
