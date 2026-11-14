using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Grading.Rules;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Grading.Services;

/// <summary>
/// Check order on save: course / term exist (400) → not finalized (409) → name unique (409) → total weight ≤ 100 (409, rule G1)
/// → max grade not below an entered score (409, update only).
/// </summary>
public sealed class GradeComponentService(IGradingRepository grading, IUnitOfWork uow) : IGradeComponentService
{
    public const string DuplicateName = "duplicate_component_name";
    public const string HasScores = "component_has_scores";
    public const string ScoresAboveMax = "scores_above_max";
    public const string Finalized = "grades_finalized";

    public async Task<GradeComponentListDto> ListAsync(GradeComponentQuery query, CancellationToken ct)
    {
        await EnsureReferencesExistAsync(query.CourseId, query.TermId, ct);
        var items = await grading.ListComponentsAsync(query.CourseId, query.TermId, ct);
        var finalized = await grading.IsFinalizedAsync(query.CourseId, query.TermId, ct);
        return new GradeComponentListDto(query.CourseId, query.TermId, items.Sum(i => i.Weight), finalized, items);
    }

    public async Task<GradeComponentDto> GetAsync(int id, CancellationToken ct) =>
        await grading.GetComponentDtoAsync(id, ct) ?? throw NotFoundException.For<GradeComponent>(id);

    public async Task<GradeComponentDto> CreateAsync(CreateGradeComponentRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        await EnsureReferencesExistAsync(request.CourseId, request.TermId, ct);
        await EnsureNotFinalizedAsync(request.CourseId, request.TermId, ct);
        await EnsureNameFreeAsync(request.CourseId, request.TermId, name, excludeId: null, ct);
        GradeComponentPolicy.CheckTotalWeight(await grading.OtherWeightsAsync(request.CourseId, request.TermId, null, ct), request.Weight);

        var component = new GradeComponent
        {
            CourseId = request.CourseId, TermId = request.TermId, Name = name, MaxGrade = request.MaxGrade, Weight = request.Weight,
        };
        grading.Add(component);
        await uow.SaveChangesAsync(ct);
        return await GetAsync(component.Id, ct);
    }

    public async Task<GradeComponentDto> UpdateAsync(int id, UpdateGradeComponentRequest request, CancellationToken ct)
    {
        var component = await grading.FindComponentAsync(id, ct) ?? throw NotFoundException.For<GradeComponent>(id);
        var name = request.Name.Trim();
        await EnsureNotFinalizedAsync(component.CourseId, component.TermId, ct);
        await EnsureNameFreeAsync(component.CourseId, component.TermId, name, excludeId: id, ct);
        GradeComponentPolicy.CheckTotalWeight(await grading.OtherWeightsAsync(component.CourseId, component.TermId, id, ct), request.Weight);

        if (await grading.HighestScoreAsync(id, ct) is { } highest && highest > request.MaxGrade)
            throw new BusinessRuleException(
                $"A student already scored {highest:0.##} on {component.Name}, so the max grade can't be lower than that.", ScoresAboveMax);

        component.Name = name;
        component.MaxGrade = request.MaxGrade;
        component.Weight = request.Weight;
        await uow.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var component = await grading.FindComponentAsync(id, ct) ?? throw NotFoundException.For<GradeComponent>(id);
        await EnsureNotFinalizedAsync(component.CourseId, component.TermId, ct);
        var scores = await grading.CountScoresAsync(id, ct);
        if (scores > 0)
            throw new BusinessRuleException(
                $"{component.Name} already has {scores} score{(scores == 1 ? "" : "s")}, so it can't be deleted.", HasScores);

        grading.Remove(component);
        await uow.SaveChangesAsync(ct);
    }

    private async Task EnsureReferencesExistAsync(int courseId, int termId, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        if (!await grading.CourseExistsAsync(courseId, ct)) failures.Add(new("courseId", $"Course {courseId} doesn't exist."));
        if (!await grading.TermExistsAsync(termId, ct)) failures.Add(new("termId", $"Term {termId} doesn't exist."));
        if (failures.Count > 0) throw new ValidationException(failures);
    }

    private async Task EnsureNotFinalizedAsync(int courseId, int termId, CancellationToken ct)
    {
        if (await grading.IsFinalizedAsync(courseId, termId, ct))
            throw new BusinessRuleException("Grades for this course were already finalized this term, so its components are locked.", Finalized);
    }

    private async Task EnsureNameFreeAsync(int courseId, int termId, string name, int? excludeId, CancellationToken ct)
    {
        if (await grading.ComponentNameExistsAsync(courseId, termId, name, excludeId, ct))
            throw new BusinessRuleException($"This course already has a component called \"{name}\" this term.", DuplicateName);
    }
}
