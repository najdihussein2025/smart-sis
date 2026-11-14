using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Courses.Models.Dtos;

namespace SmartSis.Api.Features.Courses.Interfaces;

/// <summary>Student course catalog for the current term (PB-04, T13).</summary>
public interface ICatalogService
{
    Task<CatalogDto> ListAsync(CatalogQuery query, CancellationToken ct);
    Task<CatalogCourseDetailDto> GetCourseAsync(int courseId, CancellationToken ct);
    Task<IReadOnlyList<CatalogMajorDto>> ListMajorsAsync(CancellationToken ct);
}

/// <summary>The current term, for the catalog.</summary>
public sealed record CatalogTerm(int Id, string Name, DateTime EnrollStart, DateTime EnrollEnd, DateTime AddDropStart, DateTime AddDropEnd);

public interface ICatalogRepository
{
    Task<CatalogTerm?> GetCurrentTermAsync(CancellationToken ct);
    Task<int?> GetStudentMajorIdAsync(int studentId, CancellationToken ct);

    /// <summary>Courses with at least one section in the term, optionally only those mapped to a major (and type).</summary>
    Task<List<CatalogCourseDto>> ListCoursesAsync(int termId, int? majorId, CourseType? type, string? search, int studentId, CancellationToken ct);

    Task<CatalogCourseDetailDto?> GetCourseAsync(int courseId, int? termId, int studentId, CancellationToken ct);

    Task<List<CatalogMajorDto>> ListMajorsAsync(CancellationToken ct);
}
