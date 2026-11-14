using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Models.Entities;
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
}
