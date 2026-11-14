using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Career.Interfaces;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Career.Services;

/// <summary>
/// Admin side of the career module. Check order on save: referenced ids exist (400) → name unique (409)
/// → history allows the change (409: an answered question can't be deleted, a chosen option can't be removed).
/// </summary>
public sealed class CareerAdminService(ICareerRepository repo, IUnitOfWork uow) : ICareerAdminService
{
    // Traits

    public async Task<IReadOnlyList<TraitDto>> ListTraitsAsync(CancellationToken ct) => await repo.ListTraitsAsync(ct);

    public async Task<TraitDto> CreateTraitAsync(SaveTraitRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        await EnsureTraitNameFreeAsync(name, null, ct);
        var trait = new CareerTrait { Name = name, Description = Clean(request.Description) };
        repo.Add(trait);
        await uow.SaveChangesAsync(ct);
        return new TraitDto(trait.Id, trait.Name, trait.Description);
    }

    public async Task<TraitDto> UpdateTraitAsync(int id, SaveTraitRequest request, CancellationToken ct)
    {
        var trait = await repo.FindTraitAsync(id, ct) ?? throw NotFoundException.For<CareerTrait>(id);
        var name = request.Name.Trim();
        await EnsureTraitNameFreeAsync(name, id, ct);
        trait.Name = name;
        trait.Description = Clean(request.Description);
        await uow.SaveChangesAsync(ct);
        return new TraitDto(trait.Id, trait.Name, trait.Description);
    }

    private async Task EnsureTraitNameFreeAsync(string name, int? excludeId, CancellationToken ct)
    {
        if (await repo.TraitNameExistsAsync(name, excludeId, ct))
            throw new BusinessRuleException($"A trait named \"{name}\" already exists.", "duplicate_trait");
    }

    // Question bank

    public async Task<QuestionBankDto> ListQuestionsAsync(CancellationToken ct)
    {
        var questions = await repo.ListQuestionsAsync(null, ct);
        return new QuestionBankDto(
            questions.Count(q => q.IsActive), Rules.AssessmentScoring.MinQuestions, Rules.AssessmentScoring.MaxQuestions, questions);
    }

    public async Task<QuestionDto> GetQuestionAsync(int id, CancellationToken ct) =>
        (await repo.ListQuestionsAsync(id, ct)).SingleOrDefault() ?? throw NotFoundException.For<CareerQuestion>(id);

    public async Task<QuestionDto> CreateQuestionAsync(SaveQuestionRequest request, CancellationToken ct)
    {
        await EnsureTraitsExistAsync(request, ct);
        var question = new CareerQuestion { Text = request.Text.Trim(), IsActive = request.IsActive };
        for (var i = 0; i < request.Options.Length; i++)
            question.Options.Add(NewOption(request.Options[i], i + 1));
        repo.Add(question);
        await uow.SaveChangesAsync(ct);
        return await GetQuestionAsync(question.Id, ct);
    }

    public async Task<QuestionDto> UpdateQuestionAsync(int id, SaveQuestionRequest request, CancellationToken ct)
    {
        var question = await repo.GetQuestionForUpdateAsync(id, ct) ?? throw NotFoundException.For<CareerQuestion>(id);
        await EnsureTraitsExistAsync(request, ct);

        var own = question.Options.ToDictionary(o => o.Id);
        var foreign = request.Options.Select((o, i) => (o, i))
            .Where(x => x.o.Id is { } oid && !own.ContainsKey(oid))
            .Select(x => new ValidationFailure($"options[{x.i}].id", $"Option {x.o.Id} isn't part of this question."))
            .ToList();
        if (foreign.Count > 0) throw new ValidationException(foreign);

        var keep = request.Options.Where(o => o.Id is not null).Select(o => o.Id!.Value).ToHashSet();
        var removed = question.Options.Where(o => !keep.Contains(o.Id)).ToList();
        var chosen = await repo.OptionIdsWithAnswersAsync(removed.Select(o => o.Id), ct);
        if (removed.FirstOrDefault(o => chosen.Contains(o.Id)) is { } locked)
            throw new BusinessRuleException(
                $"Students already chose \"{locked.Text}\", so it can't be removed. Deactivate the question and add a new one instead.",
                "option_answered");

        question.Text = request.Text.Trim();
        question.IsActive = request.IsActive;

        foreach (var option in removed)
        {
            foreach (var p in option.TraitPoints.ToList()) repo.Remove(p);
            repo.Remove(option);
            question.Options.Remove(option);
        }

        for (var i = 0; i < request.Options.Length; i++)
        {
            var r = request.Options[i];
            if (r.Id is { } oid)
            {
                var option = own[oid];
                option.Text = r.Text.Trim();
                option.OptionOrder = i + 1;
                ReplacePoints(option, r.Points);
            }
            else question.Options.Add(NewOption(r, i + 1));
        }

        await uow.SaveChangesAsync(ct);
        return await GetQuestionAsync(id, ct);
    }

    public async Task DeleteQuestionAsync(int id, CancellationToken ct)
    {
        var question = await repo.GetQuestionForUpdateAsync(id, ct) ?? throw NotFoundException.For<CareerQuestion>(id);
        if (await repo.CountAnswersAsync(id, ct) > 0)
            throw new BusinessRuleException(
                "Students have answered this question, so it can't be deleted. Deactivate it instead.", "question_answered");

        foreach (var option in question.Options)
        {
            foreach (var p in option.TraitPoints) repo.Remove(p);
            repo.Remove(option);
        }
        repo.Remove(question);
        await uow.SaveChangesAsync(ct);
    }

    private static CareerQuestionOption NewOption(SaveOptionRequest r, int order)
    {
        var option = new CareerQuestionOption { Text = r.Text.Trim(), OptionOrder = order };
        foreach (var p in r.Points) option.TraitPoints.Add(new CareerOptionTraitPoint { TraitId = p.TraitId, Points = p.Points });
        return option;
    }

    private void ReplacePoints(CareerQuestionOption option, SaveTraitPointsRequest[] points)
    {
        var wanted = points.ToDictionary(p => p.TraitId, p => p.Points);
        foreach (var p in option.TraitPoints.Where(p => !wanted.ContainsKey(p.TraitId)).ToList())
        {
            repo.Remove(p);
            option.TraitPoints.Remove(p);
        }
        foreach (var (traitId, value) in wanted)
        {
            var existing = option.TraitPoints.FirstOrDefault(p => p.TraitId == traitId);
            if (existing is not null) existing.Points = value;
            else option.TraitPoints.Add(new CareerOptionTraitPoint { TraitId = traitId, Points = value });
        }
    }

    private async Task EnsureTraitsExistAsync(SaveQuestionRequest r, CancellationToken ct)
    {
        var exist = await repo.ExistingTraitIdsAsync(r.Options.SelectMany(o => o.Points).Select(p => p.TraitId), ct);
        var failures = new List<ValidationFailure>();
        for (var i = 0; i < r.Options.Length; i++)
            for (var j = 0; j < r.Options[i].Points.Length; j++)
                if (!exist.Contains(r.Options[i].Points[j].TraitId))
                    failures.Add(new($"options[{i}].points[{j}].traitId", $"Trait {r.Options[i].Points[j].TraitId} doesn't exist."));
        if (failures.Count > 0) throw new ValidationException(failures);
    }

    // Careers

    public async Task<IReadOnlyList<CareerPathDto>> ListCareersAsync(CancellationToken ct) => await repo.ListCareersAsync(null, ct);

    public async Task<CareerPathDto> GetCareerAsync(int id, CancellationToken ct) =>
        (await repo.ListCareersAsync(id, ct)).SingleOrDefault() ?? throw NotFoundException.For<CareerPath>(id);

    public async Task<CareerPathDto> CreateCareerAsync(SaveCareerPathRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        await EnsureCareerReferencesAsync(request, ct);
        await EnsureCareerNameFreeAsync(name, null, ct);

        var career = new CareerPath { Name = name };
        Apply(career, request);
        repo.Add(career);
        await uow.SaveChangesAsync(ct);
        return await GetCareerAsync(career.Id, ct);
    }

    public async Task<CareerPathDto> UpdateCareerAsync(int id, SaveCareerPathRequest request, CancellationToken ct)
    {
        var career = await repo.GetCareerForUpdateAsync(id, ct) ?? throw NotFoundException.For<CareerPath>(id);
        var name = request.Name.Trim();
        await EnsureCareerReferencesAsync(request, ct);
        await EnsureCareerNameFreeAsync(name, id, ct);

        career.Name = name;
        Apply(career, request);
        await uow.SaveChangesAsync(ct);
        return await GetCareerAsync(id, ct);
    }

    /// <summary>Replaces weights (matched by trait) and steps (matched by position, so StepOrder stays unique while saving).</summary>
    private void Apply(CareerPath career, SaveCareerPathRequest r)
    {
        career.Description = Clean(r.Description);
        career.IsActive = r.IsActive;

        var weights = r.Traits.ToDictionary(t => t.TraitId, t => t.Weight);
        foreach (var t in career.Traits.Where(t => !weights.ContainsKey(t.TraitId)).ToList())
        {
            repo.Remove(t);
            career.Traits.Remove(t);
        }
        foreach (var (traitId, weight) in weights)
        {
            var existing = career.Traits.FirstOrDefault(t => t.TraitId == traitId);
            if (existing is not null) existing.Weight = weight;
            else career.Traits.Add(new CareerPathTrait { TraitId = traitId, Weight = weight });
        }

        var steps = career.Steps.OrderBy(s => s.StepOrder).ToList();
        foreach (var extra in steps.Skip(r.Steps.Length))
        {
            repo.Remove(extra);
            career.Steps.Remove(extra);
        }
        for (var i = 0; i < r.Steps.Length; i++)
        {
            var step = i < steps.Count ? steps[i] : new CareerRoadmapStep();
            step.StepOrder = i + 1;
            step.Title = r.Steps[i].Title.Trim();
            step.Description = Clean(r.Steps[i].Description);
            step.CourseId = r.Steps[i].CourseId;
            if (i >= steps.Count) career.Steps.Add(step);
        }
    }

    private async Task EnsureCareerReferencesAsync(SaveCareerPathRequest r, CancellationToken ct)
    {
        var traits = await repo.ExistingTraitIdsAsync(r.Traits.Select(t => t.TraitId), ct);
        var courses = await repo.ExistingCourseIdsAsync(r.Steps.Where(s => s.CourseId is not null).Select(s => s.CourseId!.Value), ct);
        var failures = new List<ValidationFailure>();
        for (var i = 0; i < r.Traits.Length; i++)
            if (!traits.Contains(r.Traits[i].TraitId))
                failures.Add(new($"traits[{i}].traitId", $"Trait {r.Traits[i].TraitId} doesn't exist."));
        for (var i = 0; i < r.Steps.Length; i++)
            if (r.Steps[i].CourseId is { } cid && !courses.Contains(cid))
                failures.Add(new($"steps[{i}].courseId", $"Course {cid} doesn't exist."));
        if (failures.Count > 0) throw new ValidationException(failures);
    }

    private async Task EnsureCareerNameFreeAsync(string name, int? excludeId, CancellationToken ct)
    {
        if (await repo.CareerNameExistsAsync(name, excludeId, ct))
            throw new BusinessRuleException($"A career named \"{name}\" already exists.", "duplicate_career");
    }

    // Course tags

    public async Task<IReadOnlyList<CourseTagsDto>> ListCourseTagsAsync(CancellationToken ct) => await repo.ListCourseTagsAsync(null, ct);

    public async Task<CourseTagsDto> SaveCourseTagsAsync(int courseId, SaveCourseTagsRequest request, CancellationToken ct)
    {
        if ((await repo.ExistingCourseIdsAsync([courseId], ct)).Count == 0) throw NotFoundException.For<Course>(courseId);
        var exist = await repo.ExistingTraitIdsAsync(request.TraitIds, ct);
        var failures = request.TraitIds.Select((id, i) => (id, i)).Where(x => !exist.Contains(x.id))
            .Select(x => new ValidationFailure($"traitIds[{x.i}]", $"Trait {x.id} doesn't exist.")).ToList();
        if (failures.Count > 0) throw new ValidationException(failures);

        var current = await repo.GetCourseTagsForUpdateAsync(courseId, ct);
        var wanted = request.TraitIds.ToHashSet();
        foreach (var tag in current.Where(t => !wanted.Contains(t.TraitId))) repo.Remove(tag);
        foreach (var traitId in wanted.Where(id => current.All(t => t.TraitId != id)))
            repo.Add(new CourseTrait { CourseId = courseId, TraitId = traitId });
        await uow.SaveChangesAsync(ct);
        return (await repo.ListCourseTagsAsync(courseId, ct)).Single();
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
