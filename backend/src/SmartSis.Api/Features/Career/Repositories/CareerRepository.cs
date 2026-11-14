using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Cart.Models.Entities;
using SmartSis.Api.Features.Career.Interfaces;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Career.Rules;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Career.Repositories;

/// <summary>Career queries. Reads Courses, Enrollments, Sections and CartItems; writes only Career tables.</summary>
public sealed class CareerRepository(SisDbContext db) : ICareerRepository
{
    public async Task<(int Id, string Name)?> GetCurrentTermAsync(CancellationToken ct)
    {
        var t = await db.Set<Term>().AsNoTracking().Where(x => x.IsCurrent).Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync(ct);
        return t is null ? null : (t.Id, t.Name);
    }

    public async Task<HashSet<int>> ExistingTraitIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        return [.. await db.Set<CareerTrait>().Where(t => list.Contains(t.Id)).Select(t => t.Id).ToListAsync(ct)];
    }

    public async Task<HashSet<int>> ExistingCourseIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        return [.. await db.Set<Course>().Where(c => list.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct)];
    }

    public void Add(object entity) => db.Add(entity);
    public void Remove(object entity) => db.Remove(entity);

    // Traits

    public Task<List<TraitDto>> ListTraitsAsync(CancellationToken ct) =>
        db.Set<CareerTrait>().AsNoTracking().OrderBy(t => t.Name).Select(t => new TraitDto(t.Id, t.Name, t.Description)).ToListAsync(ct);

    public Task<CareerTrait?> FindTraitAsync(int id, CancellationToken ct) =>
        db.Set<CareerTrait>().FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> TraitNameExistsAsync(string name, int? excludeId, CancellationToken ct)
    {
        var lower = name.ToLower();
        return db.Set<CareerTrait>().AnyAsync(t => t.Name.ToLower() == lower && t.Id != excludeId, ct);
    }

    // Question bank

    public async Task<List<QuestionDto>> ListQuestionsAsync(int? id, CancellationToken ct)
    {
        var answers = db.Set<CareerAssessmentAnswer>();
        var rows = await db.Set<CareerQuestion>().AsNoTracking()
            .Where(q => id == null || q.Id == id)
            .OrderByDescending(q => q.IsActive).ThenBy(q => q.Id)
            .Select(q => new
            {
                q.Id, q.Text, q.IsActive,
                AnswerCount = answers.Count(a => a.QuestionId == q.Id),
                Options = q.Options.OrderBy(o => o.OptionOrder).ThenBy(o => o.Id).Select(o => new
                {
                    o.Id, o.Text,
                    Points = o.TraitPoints.OrderBy(p => p.Trait.Name)
                        .Select(p => new TraitPointsDto(p.TraitId, p.Trait.Name, p.Points)).ToList(),
                }).ToList(),
            }).ToListAsync(ct);

        return rows.Select(q => new QuestionDto(q.Id, q.Text, q.IsActive, q.AnswerCount,
            q.Options.Select(o => new QuestionOptionDto(o.Id, o.Text, o.Points)).ToList())).ToList();
    }

    public Task<CareerQuestion?> GetQuestionForUpdateAsync(int id, CancellationToken ct) =>
        db.Set<CareerQuestion>().Include(q => q.Options).ThenInclude(o => o.TraitPoints).FirstOrDefaultAsync(q => q.Id == id, ct);

    public Task<int> CountAnswersAsync(int questionId, CancellationToken ct) =>
        db.Set<CareerAssessmentAnswer>().CountAsync(a => a.QuestionId == questionId, ct);

    public async Task<HashSet<int>> OptionIdsWithAnswersAsync(IEnumerable<int> optionIds, CancellationToken ct)
    {
        var list = optionIds.ToList();
        return [.. await db.Set<CareerAssessmentAnswer>().Where(a => list.Contains(a.OptionId)).Select(a => a.OptionId).Distinct().ToListAsync(ct)];
    }

    public Task<List<CareerQuestion>> ListActiveQuestionsAsync(CancellationToken ct) =>
        db.Set<CareerQuestion>().AsNoTracking()
            .Include(q => q.Options).ThenInclude(o => o.TraitPoints)
            .Where(q => q.IsActive)
            .OrderBy(q => q.Id)
            .AsSplitQuery()
            .ToListAsync(ct);

    // Careers

    public Task<List<CareerPathDto>> ListCareersAsync(int? id, CancellationToken ct) =>
        db.Set<CareerPath>().AsNoTracking()
            .Where(c => id == null || c.Id == id)
            .OrderBy(c => c.Name)
            .Select(c => new CareerPathDto(
                c.Id, c.Name, c.Description, c.IsActive,
                c.Traits.OrderByDescending(t => t.Weight).ThenBy(t => t.Trait.Name)
                    .Select(t => new TraitWeightDto(t.TraitId, t.Trait.Name, t.Weight)).ToList(),
                c.Steps.OrderBy(s => s.StepOrder)
                    .Select(s => new RoadmapStepDto(
                        s.Id, s.StepOrder, s.Title, s.Description, s.CourseId,
                        s.Course != null ? s.Course.Code : null, s.Course != null ? s.Course.Name : null))
                    .ToList()))
            .ToListAsync(ct);

    public Task<CareerPath?> GetCareerForUpdateAsync(int id, CancellationToken ct) =>
        db.Set<CareerPath>().Include(c => c.Traits).Include(c => c.Steps).AsSplitQuery().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> CareerNameExistsAsync(string name, int? excludeId, CancellationToken ct)
    {
        var lower = name.ToLower();
        return db.Set<CareerPath>().AnyAsync(c => c.Name.ToLower() == lower && c.Id != excludeId, ct);
    }

    public async Task<List<CareerProfile>> ActiveCareerProfilesAsync(CancellationToken ct)
    {
        var rows = await db.Set<CareerPath>().AsNoTracking().Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Name, Weights = c.Traits.Select(t => new { t.TraitId, t.Weight }).ToList() })
            .ToListAsync(ct);
        return rows.Select(r => new CareerProfile(r.Id, r.Name, r.Weights.ToDictionary(w => w.TraitId, w => w.Weight))).ToList();
    }

    public Task<List<CareerSummaryDto>> ActiveCareersAsync(CancellationToken ct) =>
        db.Set<CareerPath>().AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name)
            .Select(c => new CareerSummaryDto(c.Id, c.Name, c.Description)).ToListAsync(ct);

    public Task<CareerPath?> GetActiveCareerWithStepsAsync(int id, CancellationToken ct) =>
        db.Set<CareerPath>().AsNoTracking()
            .Include(c => c.Steps).ThenInclude(s => s.Course)
            .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

    public async Task<List<(int CareerId, int CourseId, int StepOrder)>> RoadmapCoursesAsync(IEnumerable<int> careerIds, CancellationToken ct)
    {
        var ids = careerIds.ToList();
        var rows = await db.Set<CareerRoadmapStep>().AsNoTracking()
            .Where(s => ids.Contains(s.CareerPathId) && s.CourseId != null)
            .Select(s => new { s.CareerPathId, CourseId = s.CourseId!.Value, s.StepOrder })
            .ToListAsync(ct);
        return rows.Select(r => (r.CareerPathId, r.CourseId, r.StepOrder)).ToList();
    }

    // Course tags

    public async Task<List<CourseTagsDto>> ListCourseTagsAsync(int? courseId, CancellationToken ct)
    {
        var tags = db.Set<CourseTrait>();
        var rows = await db.Set<Course>().AsNoTracking()
            .Where(c => courseId == null || c.Id == courseId)
            .OrderBy(c => c.Code)
            .Select(c => new { c.Id, c.Code, c.Name, TraitIds = tags.Where(t => t.CourseId == c.Id).Select(t => t.TraitId).ToList() })
            .ToListAsync(ct);
        return rows.Select(r => new CourseTagsDto(r.Id, r.Code, r.Name, r.TraitIds.Order().ToList())).ToList();
    }

    public Task<List<CourseTrait>> GetCourseTagsForUpdateAsync(int courseId, CancellationToken ct) =>
        db.Set<CourseTrait>().Where(t => t.CourseId == courseId).ToListAsync(ct);

    public async Task<Dictionary<int, IReadOnlyCollection<int>>> CourseTraitMapAsync(CancellationToken ct)
    {
        var rows = await db.Set<CourseTrait>().AsNoTracking().Select(t => new { t.CourseId, t.TraitId }).ToListAsync(ct);
        return rows.GroupBy(r => r.CourseId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<int>)g.Select(r => r.TraitId).ToList());
    }

    // Student

    public async Task<List<AssessmentDto>> ListAssessmentsAsync(int studentId, CancellationToken ct)
    {
        var rows = await db.Set<CareerAssessment>().AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.SubmittedAt).ThenByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id, a.TermId, TermName = a.Term.Name, a.SubmittedAt,
                Traits = a.TraitScores.Select(s => new TraitScoreDto(s.TraitId, s.Trait.Name, s.Points, s.MaxPoints, s.Score)).ToList(),
            }).ToListAsync(ct);
        return rows.Select(a => new AssessmentDto(a.Id, a.TermId, a.TermName, a.SubmittedAt,
            a.Traits.OrderByDescending(t => t.Score).ThenBy(t => t.TraitName).ToList())).ToList();
    }

    public Task<bool> HasAssessmentInTermAsync(int studentId, int termId, CancellationToken ct) =>
        db.Set<CareerAssessment>().AnyAsync(a => a.StudentId == studentId && a.TermId == termId, ct);

    /// <summary>Completed attempts that count (the latest attempt of a retaken course, rule G5).</summary>
    public async Task<List<CourseGrade>> CompletedGradesAsync(int studentId, CancellationToken ct)
    {
        var rows = await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Completed && e.CountsInGpa)
            .Select(e => new { e.Section.CourseId, e.GradePoints, e.FinalPct })
            .ToListAsync(ct);
        return rows.Select(r => new CourseGrade(r.CourseId, r.GradePoints, r.FinalPct)).ToList();
    }

    /// <summary>Rule E3: a completed attempt with D or better.</summary>
    public async Task<HashSet<int>> PassedCourseIdsAsync(int studentId, CancellationToken ct) =>
        [.. await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Completed && e.GradePoints >= 1.0m)
            .Select(e => e.Section.CourseId).Distinct().ToListAsync(ct)];

    public async Task<HashSet<int>> EnrolledCourseIdsAsync(int studentId, int termId, CancellationToken ct) =>
        [.. await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled && e.Section.TermId == termId)
            .Select(e => e.Section.CourseId).Distinct().ToListAsync(ct)];

    public Task<List<string>> PassedCourseLabelsAsync(int studentId, CancellationToken ct) =>
        db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Completed && e.GradePoints >= 1.0m)
            .Select(e => e.Section.Course.Code + " " + e.Section.Course.Name)
            .Distinct().OrderBy(x => x).ToListAsync(ct);

    public Task<List<CourseInfo>> CoursesAsync(IEnumerable<int> courseIds, CancellationToken ct)
    {
        var ids = courseIds.Distinct().ToList();
        return db.Set<Course>().AsNoTracking().Where(c => ids.Contains(c.Id))
            .Select(c => new CourseInfo(c.Id, c.Code, c.Name, c.Credits)).ToListAsync(ct);
    }

    /// <summary>Sections of the courses in the term. Seats are counted from Enrolled + Completed enrollments (rule E8).</summary>
    public async Task<List<SectionOffer>> SectionOffersAsync(int termId, IEnumerable<int> courseIds, int studentId, CancellationToken ct)
    {
        var ids = courseIds.Distinct().ToList();
        var enrollments = db.Set<Enrollment>();
        var cart = db.Set<CartItem>();
        var rows = await db.Set<Section>().AsNoTracking()
            .Where(s => s.TermId == termId && ids.Contains(s.CourseId))
            .OrderBy(s => s.SectionCode)
            .Select(s => new
            {
                s.Id, s.CourseId, s.SectionCode, s.Capacity,
                Taken = enrollments.Count(e => e.SectionId == s.Id
                                               && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed)),
                Enrolled = enrollments.Any(e => e.SectionId == s.Id && e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled),
                InCart = cart.Any(c => c.SectionId == s.Id && c.StudentId == studentId),
            }).ToListAsync(ct);

        return rows.Select(r => new SectionOffer(
            r.Id, r.CourseId, r.SectionCode, Math.Max(0, r.Capacity - r.Taken),
            r.Enrolled ? CatalogStatus.Enrolled : r.InCart ? CatalogStatus.InCart : CatalogStatus.Available)).ToList();
    }
}
