using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Interfaces;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Grading.Rules;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Grading.Services;

/// <summary>
/// Every call starts with the ownership check: the section exists (404) and the signed-in instructor teaches it (403).
/// Scores are saved per component; finalizing completes the Enrolled students through <see cref="IEnrollmentService"/>
/// (the Enrollments feature owns that entity) and rebuilds their TermResults, all in one transaction.
/// </summary>
public sealed class GradeEntryService(
    IGradingRepository grading,
    IEnrollmentService enrollments,
    ITermResultService termResults,
    ICurrentUser me,
    IUnitOfWork uow,
    TimeProvider clock) : IGradeEntryService
{
    public const string Finalized = "grades_finalized";

    public async Task<IReadOnlyList<TeachingSectionDto>> ListSectionsAsync(CancellationToken ct)
    {
        var rows = await grading.ListTeachingSectionsAsync(me.UserId, ct);
        return [.. rows.Select(r => new TeachingSectionDto(
            r.SectionId, r.CourseCode, r.CourseName, r.SectionCode, r.TermName,
            r.Enrolled + r.Completed, r.Weights.Count, r.Weights.Sum(),
            Math.Max(0, r.Enrolled * r.Weights.Count - r.ActiveScores),
            r.Enrolled == 0 && r.Completed > 0))];
    }

    public async Task<GradingSectionDto> GetSectionAsync(int sectionId, CancellationToken ct)
    {
        var section = await OwnSectionAsync(sectionId, ct);
        var components = await grading.ComponentsAsync(section.CourseId, section.TermId, ct);
        var roster = await grading.RosterAsync(sectionId, ct);
        var grades = await grading.GradesAsync([.. roster.Select(r => r.EnrollmentId)], null, ct);

        var items = components.Select(c => ToDto(c, roster.Count, grades.Count(g => g.ComponentId == c.Id))).ToList();
        return new GradingSectionDto(
            section.Id, section.CourseCode, section.CourseName, section.SectionCode, section.TermName,
            roster.Count, components.Sum(c => c.Weight), items.Sum(i => i.NotGraded), IsFinalized(roster), items);
    }

    public async Task<ComponentRosterDto> GetRosterAsync(int sectionId, int componentId, CancellationToken ct)
    {
        var section = await OwnSectionAsync(sectionId, ct);
        var component = await ComponentOfAsync(section, componentId, ct);
        return await RosterDtoAsync(section, component, ct);
    }

    public async Task<ComponentRosterDto> SaveScoresAsync(int sectionId, int componentId, SaveScoresRequest request, CancellationToken ct)
    {
        var section = await OwnSectionAsync(sectionId, ct);
        var component = await ComponentOfAsync(section, componentId, ct);
        var roster = (await grading.RosterAsync(sectionId, ct)).ToDictionary(r => r.EnrollmentId);

        if (IsFinalized([.. roster.Values]))
            throw new BusinessRuleException("Grades for this section were already finalized, so scores can't change.", Finalized);

        // Field errors (400): the enrollment must be an Enrolled student of this section, the score within 0 … max.
        var failures = new List<ValidationFailure>();
        for (var i = 0; i < request.Scores.Length; i++)
        {
            var entry = request.Scores[i];
            if (!roster.TryGetValue(entry.EnrollmentId, out var student))
                failures.Add(new($"scores[{i}].enrollmentId", "This student isn't enrolled in this section."));
            else if (student.Status != EnrollmentStatus.Enrolled)
                failures.Add(new($"scores[{i}].enrollmentId", $"{student.FullName}'s grade is already final."));
            else if (entry.Score > component.MaxGrade)
                failures.Add(new($"scores[{i}].score", $"Enter a score between 0 and {component.MaxGrade:0.##}."));
        }
        if (failures.Count > 0) throw new ValidationException(failures);

        var ids = request.Scores.Select(s => s.EnrollmentId).ToList();
        var existing = (await grading.GradesAsync(ids, componentId, ct)).ToDictionary(g => g.EnrollmentId);
        var now = clock.GetUtcNow().UtcDateTime;
        foreach (var entry in request.Scores)
        {
            existing.TryGetValue(entry.EnrollmentId, out var grade);
            if (entry.Score is null)
            {
                if (grade is not null) grading.Remove(grade);
                continue;
            }
            if (grade is null)
            {
                // Rule G2: one grade per enrollment per component (also a unique index).
                grade = new StudentGrade { EnrollmentId = entry.EnrollmentId, ComponentId = componentId };
                grading.Add(grade);
            }
            else if (grade.Score == entry.Score.Value)
            {
                continue;
            }
            grade.Score = entry.Score.Value;
            grade.GradedBy = me.UserId;
            grade.GradedAt = now;
        }

        await uow.SaveChangesAsync(ct);
        return await RosterDtoAsync(section, component, ct);
    }

    public async Task<FinalizeResultDto> FinalizeAsync(int sectionId, CancellationToken ct)
    {
        var section = await OwnSectionAsync(sectionId, ct);
        return await uow.ExecuteInTransactionAsync(async () =>
        {
            var components = await grading.ComponentsAsync(section.CourseId, section.TermId, ct);
            var roster = await grading.RosterAsync(sectionId, ct);
            var active = roster.Where(r => r.Status == EnrollmentStatus.Enrolled).ToList();
            var grades = (await grading.GradesAsync([.. active.Select(r => r.EnrollmentId)], null, ct))
                .ToDictionary(g => (g.EnrollmentId, g.ComponentId), g => g.Score);

            var missing = (
                from s in active
                from c in components
                where !grades.ContainsKey((s.EnrollmentId, c.Id))
                select new MissingScore(s.FullName, c.Name)).ToList();
            FinalizePolicy.Check(active.Count, roster.Count - active.Count, [.. components.Select(c => c.Weight)], missing);

            var scale = await grading.GradeScaleAsync(ct);
            var results = new List<FinalizedStudentDto>();
            foreach (var s in active)
            {
                var final = GradeCalculator.Final(
                    components.Select(c => new ComponentScore(c.MaxGrade, c.Weight, grades[(s.EnrollmentId, c.Id)])), scale);
                await enrollments.CompleteAsync(s.EnrollmentId, final.Pct, final.Letter, final.GradePoints, ct);
                results.Add(new FinalizedStudentDto(s.EnrollmentId, s.UniversityId, s.FullName, final.Pct, final.Letter, final.GradePoints));
            }

            // Save first so the rebuild reads the completed attempts, then rebuild each student's TermResults (rule G6).
            await uow.SaveChangesAsync(ct);
            foreach (var studentId in active.Select(s => s.StudentId).Distinct())
                await termResults.RebuildAsync(studentId, ct);

            return new FinalizeResultDto(sectionId, results);
        }, ct);
    }

    private async Task<GradingSectionInfo> OwnSectionAsync(int sectionId, CancellationToken ct)
    {
        var section = await grading.GetSectionAsync(sectionId, ct) ?? throw NotFoundException.For<Section>(sectionId);
        if (section.InstructorId != me.UserId)
            throw new ForbiddenException("You can only grade sections you teach.");
        return section;
    }

    private async Task<ComponentInfo> ComponentOfAsync(GradingSectionInfo section, int componentId, CancellationToken ct) =>
        (await grading.ComponentsAsync(section.CourseId, section.TermId, ct)).FirstOrDefault(c => c.Id == componentId)
        ?? throw new NotFoundException($"This section has no grade component {componentId}.");

    private async Task<ComponentRosterDto> RosterDtoAsync(GradingSectionInfo section, ComponentInfo component, CancellationToken ct)
    {
        var roster = await grading.RosterAsync(section.Id, ct);
        var scores = (await grading.GradesAsync([.. roster.Select(r => r.EnrollmentId)], component.Id, ct))
            .ToDictionary(g => g.EnrollmentId, g => g.Score);
        var students = roster
            .Select(r => new RosterStudentDto(r.EnrollmentId, r.UniversityId, r.FullName, r.Status, scores.TryGetValue(r.EnrollmentId, out var s) ? s : null))
            .ToList();
        return new ComponentRosterDto(
            section.Id, section.CourseCode, section.SectionCode, IsFinalized(roster), ToDto(component, roster.Count, scores.Count), students);
    }

    private static GradingComponentDto ToDto(ComponentInfo c, int students, int graded) =>
        new(c.Id, c.Name, c.MaxGrade, c.Weight, graded, Math.Max(0, students - graded));

    /// <summary>Finalized = nobody is still Enrolled and at least one student Completed.</summary>
    private static bool IsFinalized(IReadOnlyCollection<RosterRow> roster) =>
        roster.Count > 0 && roster.All(r => r.Status == EnrollmentStatus.Completed);
}
