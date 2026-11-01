using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Courses.Interfaces;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Models;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Courses.Repositories;

public sealed class CourseRepository(SisDbContext db) : Repository<Course>(db), ICourseRepository
{
    public async Task<PagedResult<CourseListItemDto>> SearchAsync(string? search, int? departmentId, PageQuery page, CancellationToken ct)
    {
        var q = Set.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower: SQL Server's collation is case-insensitive anyway, EF InMemory (tests) is not.
            var s = search.Trim().ToLower();
            q = q.Where(c => c.Code.ToLower().Contains(s) || c.Name.ToLower().Contains(s));
        }
        if (departmentId is not null)
            q = q.Where(c => c.DepartmentId == departmentId);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(c => c.Code)
            .Skip(page.Skip).Take(page.Take)
            .Select(c => new CourseListItemDto(c.Id, c.Code, c.Name, c.Credits, c.Department.Code, c.Department.Name))
            .ToListAsync(ct);
        return new PagedResult<CourseListItemDto>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public Task<CourseDetailDto?> GetDetailAsync(int id, CancellationToken ct) =>
        Set.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseDetailDto(
                c.Id, c.Code, c.Name, c.Credits, c.Description, c.DepartmentId, c.Department.Name,
                c.Prerequisites
                    .OrderBy(p => p.Prerequisite.Code)
                    .Select(p => new CourseRefDto(p.PrerequisiteId, p.Prerequisite.Code, p.Prerequisite.Name))
                    .ToList(),
                c.MajorCourses
                    .OrderBy(m => m.Major.Name)
                    .Select(m => new CourseMajorDto(m.MajorId, m.Major.Name, m.Type))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

    public Task<Course?> GetForUpdateAsync(int id, CancellationToken ct) =>
        Set.Include(c => c.Prerequisites)
            .Include(c => c.MajorCourses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct)
    {
        var upper = code.ToUpper();
        return Set.AnyAsync(c => c.Code.ToUpper() == upper && (excludeId == null || c.Id != excludeId), ct);
    }

    public Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken ct) =>
        Db.Set<Department>().AnyAsync(d => d.Id == departmentId, ct);

    public async Task<HashSet<int>> ExistingMajorIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var list = ids.ToList();
        return [.. await Db.Set<Major>().Where(m => list.Contains(m.Id)).Select(m => m.Id).ToListAsync(ct)];
    }

    public async Task<HashSet<int>> ExistingCourseIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var list = ids.ToList();
        return [.. await Set.Where(c => list.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct)];
    }

    public async Task<Dictionary<int, string>> GetCodesAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        return await Set.AsNoTracking().Where(c => list.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Code, ct);
    }

    public async Task<List<(int CourseId, int PrerequisiteId)>> GetAllPrerequisiteEdgesAsync(CancellationToken ct)
    {
        var rows = await Db.Set<CoursePrerequisite>().AsNoTracking()
            .Select(p => new { p.CourseId, p.PrerequisiteId })
            .ToListAsync(ct);
        return rows.Select(r => (r.CourseId, r.PrerequisiteId)).ToList();
    }

    public void RemovePrerequisite(CoursePrerequisite row) => Db.Set<CoursePrerequisite>().Remove(row);
    public void RemoveMajorCourse(MajorCourse row) => Db.Set<MajorCourse>().Remove(row);
}
