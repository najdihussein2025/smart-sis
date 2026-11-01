using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Courses.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    /// <summary>Paged list ordered by code; <paramref name="search"/> matches code or name. Projection, not tracked.</summary>
    Task<PagedResult<CourseListItemDto>> SearchAsync(string? search, int? departmentId, PageQuery page, CancellationToken ct);

    /// <summary>Detail projection (not tracked), or null.</summary>
    Task<CourseDetailDto?> GetDetailAsync(int id, CancellationToken ct);

    /// <summary>Tracked course with its prerequisite and major rows, for updating.</summary>
    Task<Course?> GetForUpdateAsync(int id, CancellationToken ct);

    /// <summary>Case-insensitive code check (code already normalized), optionally ignoring one course.</summary>
    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct);

    Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken ct);
    Task<HashSet<int>> ExistingMajorIdsAsync(IEnumerable<int> ids, CancellationToken ct);
    Task<HashSet<int>> ExistingCourseIdsAsync(IEnumerable<int> ids, CancellationToken ct);

    /// <summary>Codes by course id, for messages.</summary>
    Task<Dictionary<int, string>> GetCodesAsync(IEnumerable<int> ids, CancellationToken ct);

    /// <summary>Every prerequisite edge (the table is small).</summary>
    Task<List<(int CourseId, int PrerequisiteId)>> GetAllPrerequisiteEdgesAsync(CancellationToken ct);

    void RemovePrerequisite(CoursePrerequisite row);
    void RemoveMajorCourse(MajorCourse row);
}
