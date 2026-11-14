using SmartSis.Api.Features.Courses.Interfaces;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Courses.Services;

/// <summary>Read-only catalog. The student comes from the token; statuses ("InCart", "Enrolled") are theirs.</summary>
public sealed class CatalogService(ICatalogRepository catalog, ICurrentUser me, TimeProvider clock) : ICatalogService
{
    public async Task<CatalogDto> ListAsync(CatalogQuery query, CancellationToken ct)
    {
        var term = await catalog.GetCurrentTermAsync(ct);
        var majorId = query.MajorId ?? await catalog.GetStudentMajorIdAsync(me.UserId, ct);
        if (term is null) return new CatalogDto(null, null, "Closed", majorId, []);

        var courses = await catalog.ListCoursesAsync(term.Id, majorId, query.Type, query.Search?.Trim(), me.UserId, ct);
        return new CatalogDto(term.Id, term.Name, Window(term), majorId, courses);
    }

    public async Task<CatalogCourseDetailDto> GetCourseAsync(int courseId, CancellationToken ct)
    {
        var term = await catalog.GetCurrentTermAsync(ct);
        return await catalog.GetCourseAsync(courseId, term?.Id, me.UserId, ct) ?? throw NotFoundException.For<Course>(courseId);
    }

    public async Task<IReadOnlyList<CatalogMajorDto>> ListMajorsAsync(CancellationToken ct) => await catalog.ListMajorsAsync(ct);

    private string Window(CatalogTerm t) => new Term
    {
        EnrollStart = t.EnrollStart, EnrollEnd = t.EnrollEnd, AddDropStart = t.AddDropStart, AddDropEnd = t.AddDropEnd,
    }.WindowAt(clock.GetUtcNow().UtcDateTime).ToString();
}
