using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Courses.Interfaces;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Courses.Rules;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Courses.Services;

/// <summary>
/// Check order on save: referenced IDs exist (400) → code unique (409) → not its own prerequisite (409) → no cycle (409).
/// </summary>
public sealed class CourseAdminService(ICourseRepository courses, IUnitOfWork uow) : ICourseAdminService
{
    public Task<PagedResult<CourseListItemDto>> SearchAsync(CourseSearchQuery query, CancellationToken ct) =>
        courses.SearchAsync(query.Search, query.DepartmentId, new PageQuery(query.Page, query.PageSize), ct);

    public async Task<CourseDetailDto> GetAsync(int id, CancellationToken ct) =>
        await courses.GetDetailAsync(id, ct) ?? throw NotFoundException.For<Course>(id);

    public async Task<CourseDetailDto> CreateAsync(SaveCourseRequest request, CancellationToken ct)
    {
        var code = CourseCode.Normalize(request.Code);
        await EnsureReferencesExistAsync(request, ct);
        await EnsureCodeFreeAsync(code, excludeId: null, ct);
        // A new course has no incoming edges, so it can't close a prerequisite cycle.

        var course = new Course
        {
            Code = code,
            Name = request.Name.Trim(),
            Credits = request.Credits,
            Description = Clean(request.Description),
            DepartmentId = request.DepartmentId,
            Prerequisites = [.. request.PrerequisiteIds.Select(id => new CoursePrerequisite { PrerequisiteId = id })],
            MajorCourses = [.. request.Majors.Select(m => new MajorCourse { MajorId = m.MajorId, Type = m.Type })],
        };
        courses.Add(course);
        await uow.SaveChangesAsync(ct);

        return await GetAsync(course.Id, ct);
    }

    public async Task<CourseDetailDto> UpdateAsync(int id, SaveCourseRequest request, CancellationToken ct)
    {
        var course = await courses.GetForUpdateAsync(id, ct) ?? throw NotFoundException.For<Course>(id);
        var code = CourseCode.Normalize(request.Code);
        await EnsureReferencesExistAsync(request, ct);
        await EnsureCodeFreeAsync(code, excludeId: id, ct);

        if (request.PrerequisiteIds.Contains(id))
            throw new BusinessRuleException($"{code} can't be its own prerequisite.", "prerequisite_self");
        await EnsureNoCycleAsync(id, code, request.PrerequisiteIds, ct);

        course.Code = code;
        course.Name = request.Name.Trim();
        course.Credits = request.Credits;
        course.Description = Clean(request.Description);
        course.DepartmentId = request.DepartmentId;
        ReplacePrerequisites(course, request.PrerequisiteIds);
        ReplaceMajors(course, request.Majors);

        // One SaveChanges = one database transaction for the course and both join tables.
        await uow.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private void ReplacePrerequisites(Course course, int[] wanted)
    {
        foreach (var row in course.Prerequisites.Where(p => !wanted.Contains(p.PrerequisiteId)).ToList())
        {
            courses.RemovePrerequisite(row);
            course.Prerequisites.Remove(row);
        }
        var have = course.Prerequisites.Select(p => p.PrerequisiteId).ToHashSet();
        foreach (var prerequisiteId in wanted.Where(p => !have.Contains(p)))
            course.Prerequisites.Add(new CoursePrerequisite { CourseId = course.Id, PrerequisiteId = prerequisiteId });
    }

    private void ReplaceMajors(Course course, CourseMajorRequest[] wanted)
    {
        var byMajor = wanted.ToDictionary(m => m.MajorId, m => m.Type);
        foreach (var row in course.MajorCourses.ToList())
        {
            if (byMajor.TryGetValue(row.MajorId, out var type))
            {
                row.Type = type;
                continue;
            }
            courses.RemoveMajorCourse(row);
            course.MajorCourses.Remove(row);
        }
        var have = course.MajorCourses.Select(m => m.MajorId).ToHashSet();
        foreach (var m in wanted.Where(m => !have.Contains(m.MajorId)))
            course.MajorCourses.Add(new MajorCourse { CourseId = course.Id, MajorId = m.MajorId, Type = m.Type });
    }

    private async Task EnsureNoCycleAsync(int courseId, string code, int[] prerequisiteIds, CancellationToken ct)
    {
        var edges = (await courses.GetAllPrerequisiteEdgesAsync(ct))
            .Where(e => e.CourseId != courseId)
            .Concat(prerequisiteIds.Select(p => (courseId, p)))
            .ToList();

        var cycle = PrerequisiteCycleCheck.FindCycleThrough(edges, courseId);
        if (cycle is null) return;

        // cycle = [this, required, ..., this]
        var codes = await courses.GetCodesAsync(cycle, ct);
        string C(int id) => id == courseId ? code : codes.GetValueOrDefault(id, $"#{id}");
        var required = C(cycle[1]);
        var message = $"{code} can't require {required}: {required} already depends on {code}";
        if (cycle.Count > 3)
            message += $" ({string.Join(" → ", cycle.Skip(1).Select(C))})";
        throw new BusinessRuleException(message + ".", "prerequisite_cycle");
    }

    private async Task EnsureReferencesExistAsync(SaveCourseRequest r, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();

        if (!await courses.DepartmentExistsAsync(r.DepartmentId, ct))
            failures.Add(new("departmentId", $"Department {r.DepartmentId} doesn't exist."));

        var majors = await courses.ExistingMajorIdsAsync(r.Majors.Select(m => m.MajorId), ct);
        for (var i = 0; i < r.Majors.Length; i++)
            if (!majors.Contains(r.Majors[i].MajorId))
                failures.Add(new($"majors[{i}].majorId", $"Major {r.Majors[i].MajorId} doesn't exist."));

        var prerequisites = await courses.ExistingCourseIdsAsync(r.PrerequisiteIds, ct);
        for (var i = 0; i < r.PrerequisiteIds.Length; i++)
            if (!prerequisites.Contains(r.PrerequisiteIds[i]))
                failures.Add(new($"prerequisiteIds[{i}]", $"Course {r.PrerequisiteIds[i]} doesn't exist."));

        if (failures.Count > 0) throw new ValidationException(failures);
    }

    private async Task EnsureCodeFreeAsync(string code, int? excludeId, CancellationToken ct)
    {
        if (await courses.CodeExistsAsync(code, excludeId, ct))
            throw new BusinessRuleException($"Course code {code} already exists.", "duplicate_course_code");
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
