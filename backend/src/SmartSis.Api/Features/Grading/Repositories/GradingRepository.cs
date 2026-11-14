using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Grading.Rules;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Grading.Repositories;

public sealed class GradingRepository(SisDbContext db) : IGradingRepository
{
    private DbSet<GradeComponent> Components => db.Set<GradeComponent>();

    // ---- Components (admin) ----

    public Task<bool> CourseExistsAsync(int courseId, CancellationToken ct) => db.Set<Course>().AnyAsync(c => c.Id == courseId, ct);

    public Task<bool> TermExistsAsync(int termId, CancellationToken ct) => db.Set<Term>().AnyAsync(t => t.Id == termId, ct);

    public Task<List<GradeComponentDto>> ListComponentsAsync(int courseId, int termId, CancellationToken ct) =>
        ProjectComponents(Components.Where(c => c.CourseId == courseId && c.TermId == termId).OrderBy(c => c.Id)).ToListAsync(ct);

    public Task<GradeComponentDto?> GetComponentDtoAsync(int id, CancellationToken ct) =>
        ProjectComponents(Components.Where(c => c.Id == id)).FirstOrDefaultAsync(ct);

    private IQueryable<GradeComponentDto> ProjectComponents(IQueryable<GradeComponent> q) =>
        q.AsNoTracking().Select(c => new GradeComponentDto(
            c.Id, c.CourseId, c.TermId, c.Name, c.MaxGrade, c.Weight,
            db.Set<StudentGrade>().Count(g => g.ComponentId == c.Id)));

    public Task<GradeComponent?> FindComponentAsync(int id, CancellationToken ct) => Components.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<decimal>> OtherWeightsAsync(int courseId, int termId, int? excludeId, CancellationToken ct) =>
        Components.Where(c => c.CourseId == courseId && c.TermId == termId && c.Id != excludeId).Select(c => c.Weight).ToListAsync(ct);

    public Task<bool> ComponentNameExistsAsync(int courseId, int termId, string name, int? excludeId, CancellationToken ct)
    {
        var lower = name.ToLower();
        return Components.AnyAsync(c => c.CourseId == courseId && c.TermId == termId && c.Name.ToLower() == lower && c.Id != excludeId, ct);
    }

    public Task<int> CountScoresAsync(int componentId, CancellationToken ct) =>
        db.Set<StudentGrade>().CountAsync(g => g.ComponentId == componentId, ct);

    public Task<decimal?> HighestScoreAsync(int componentId, CancellationToken ct) =>
        db.Set<StudentGrade>().Where(g => g.ComponentId == componentId).MaxAsync(g => (decimal?)g.Score, ct);

    public Task<bool> IsFinalizedAsync(int courseId, int termId, CancellationToken ct) =>
        db.Set<Enrollment>().AnyAsync(e => e.Section.CourseId == courseId && e.Section.TermId == termId
                                           && e.Status == EnrollmentStatus.Completed, ct);

    public void Add(GradeComponent component) => Components.Add(component);

    public void Remove(GradeComponent component) => Components.Remove(component);

    // ---- Grade entry (instructor) ----

    public Task<GradingSectionInfo?> GetSectionAsync(int sectionId, CancellationToken ct) =>
        db.Set<Section>().AsNoTracking().Where(s => s.Id == sectionId)
            .Select(s => new GradingSectionInfo(
                s.Id, s.CourseId, s.TermId, s.InstructorId, s.Course.Code, s.Course.Name, s.SectionCode, s.Term.Name))
            .FirstOrDefaultAsync(ct);

    public async Task<List<TeachingSectionRow>> ListTeachingSectionsAsync(int instructorId, CancellationToken ct)
    {
        var rows = await db.Set<Section>().AsNoTracking()
            .Where(s => s.InstructorId == instructorId && s.Term.IsCurrent)
            .OrderBy(s => s.Course.Code).ThenBy(s => s.SectionCode)
            .Select(s => new
            {
                s.Id, s.Course.Code, s.Course.Name, s.SectionCode, TermName = s.Term.Name,
                Enrolled = db.Set<Enrollment>().Count(e => e.SectionId == s.Id && e.Status == EnrollmentStatus.Enrolled),
                Completed = db.Set<Enrollment>().Count(e => e.SectionId == s.Id && e.Status == EnrollmentStatus.Completed),
                Weights = db.Set<GradeComponent>().Where(c => c.CourseId == s.CourseId && c.TermId == s.TermId).Select(c => c.Weight).ToList(),
                ActiveScores = db.Set<StudentGrade>().Count(g => g.Enrollment.SectionId == s.Id && g.Enrollment.Status == EnrollmentStatus.Enrolled),
            })
            .ToListAsync(ct);
        return [.. rows.Select(r => new TeachingSectionRow(r.Id, r.Code, r.Name, r.SectionCode, r.TermName, r.Enrolled, r.Completed, r.Weights, r.ActiveScores))];
    }

    public Task<List<ComponentInfo>> ComponentsAsync(int courseId, int termId, CancellationToken ct) =>
        Components.AsNoTracking().Where(c => c.CourseId == courseId && c.TermId == termId).OrderBy(c => c.Id)
            .Select(c => new ComponentInfo(c.Id, c.Name, c.MaxGrade, c.Weight))
            .ToListAsync(ct);

    public Task<List<RosterRow>> RosterAsync(int sectionId, CancellationToken ct) =>
        db.Set<Enrollment>().IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.SectionId == sectionId && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed))
            .OrderBy(e => e.Student.User.FullName).ThenBy(e => e.Student.User.UniversityId)
            .Select(e => new RosterRow(e.Id, e.StudentId, e.Student.User.UniversityId, e.Student.User.FullName, e.Status))
            .ToListAsync(ct);

    public Task<List<StudentGrade>> GradesAsync(IReadOnlyCollection<int> enrollmentIds, int? componentId, CancellationToken ct) =>
        db.Set<StudentGrade>()
            .Where(g => enrollmentIds.Contains(g.EnrollmentId) && (componentId == null || g.ComponentId == componentId))
            .ToListAsync(ct);

    public Task<List<GradeBand>> GradeScaleAsync(CancellationToken ct) =>
        db.Set<GradeScale>().AsNoTracking().OrderByDescending(g => g.MinPct)
            .Select(g => new GradeBand(g.Letter, g.GpaPoints, g.MinPct, g.MaxPct))
            .ToListAsync(ct);

    public void Add(StudentGrade grade) => db.Set<StudentGrade>().Add(grade);

    public void Remove(StudentGrade grade) => db.Set<StudentGrade>().Remove(grade);

    // ---- Term results (rule G6) ----

    public async Task<List<AttemptRow>> GradedAttemptsAsync(int studentId, CancellationToken ct)
    {
        var rows = await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.GradePoints != null
                        && (e.Status == EnrollmentStatus.Completed || e.Status == EnrollmentStatus.DroppedAbsence))
            .OrderBy(e => e.Id)
            .Select(e => new { e.Section.TermId, e.Section.CourseId, e.Section.Course.Credits, e.GradePoints, e.Section.Term.StartDate })
            .ToListAsync(ct);
        return [.. rows.Select(r => new AttemptRow(new GradedAttempt(r.TermId, r.CourseId, r.Credits, r.GradePoints!.Value), r.StartDate))];
    }

    public Task<List<TermResult>> TermResultsAsync(int studentId, CancellationToken ct) =>
        db.Set<TermResult>().Where(r => r.StudentId == studentId).ToListAsync(ct);

    public void Add(TermResult result) => db.Set<TermResult>().Add(result);

    public void Remove(TermResult result) => db.Set<TermResult>().Remove(result);
}
