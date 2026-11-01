using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Courses.Interfaces;

/// <summary>Admin management of courses, prerequisites and major mapping (PB-02, SR-21).</summary>
public interface ICourseAdminService
{
    Task<PagedResult<CourseListItemDto>> SearchAsync(CourseSearchQuery query, CancellationToken ct);
    Task<CourseDetailDto> GetAsync(int id, CancellationToken ct);
    Task<CourseDetailDto> CreateAsync(SaveCourseRequest request, CancellationToken ct);
    Task<CourseDetailDto> UpdateAsync(int id, SaveCourseRequest request, CancellationToken ct);
}
