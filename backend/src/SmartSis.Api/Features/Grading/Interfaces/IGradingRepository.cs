using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Grading.Rules;

namespace SmartSis.Api.Features.Grading.Interfaces;

/// <summary>The facts about a section that grading needs.</summary>
public sealed record GradingSectionInfo(
    int Id, int CourseId, int TermId, int? InstructorId, string CourseCode, string CourseName, string SectionCode, string TermName);

/// <summary>A component's numbers.</summary>
public sealed record ComponentInfo(int Id, string Name, decimal MaxGrade, decimal Weight);

/// <summary>An Enrolled or Completed student of a section.</summary>
public sealed record RosterRow(int EnrollmentId, int StudentId, long UniversityId, string FullName, EnrollmentStatus Status);

/// <summary>Counts for the instructor's section list.</summary>
public sealed record TeachingSectionRow(
    int SectionId, string CourseCode, string CourseName, string SectionCode, string TermName,
    int Enrolled, int Completed, IReadOnlyList<decimal> Weights, int ActiveScores);

/// <summary>A graded attempt with the start date of its term, for ordering.</summary>
public sealed record AttemptRow(GradedAttempt Attempt, DateOnly TermStart);

/// <summary>One of the student's enrollments (not plain Dropped), for the grades and transcript views.</summary>
public sealed record StudentCourseRow(
    int EnrollmentId, int SectionId, int CourseId, string CourseCode, string CourseName, string SectionCode, int Credits,
    int TermId, string TermName, DateOnly TermStart, bool TermIsCurrent,
    EnrollmentStatus Status, decimal? FinalPct, string? LetterGrade, decimal? GradePoints, bool CountsInGpa);

/// <summary>A component with its course, for a whole term at once.</summary>
public sealed record TermComponentRow(int CourseId, ComponentInfo Component);

/// <summary>EF queries for the Grading feature. No SaveChanges here.</summary>
public interface IGradingRepository
{
    // ---- Components (admin) ----
    Task<bool> CourseExistsAsync(int courseId, CancellationToken ct);
    Task<bool> TermExistsAsync(int termId, CancellationToken ct);
    Task<List<GradeComponentDto>> ListComponentsAsync(int courseId, int termId, CancellationToken ct);
    Task<GradeComponentDto?> GetComponentDtoAsync(int id, CancellationToken ct);

    /// <summary>Tracked.</summary>
    Task<GradeComponent?> FindComponentAsync(int id, CancellationToken ct);

    Task<List<decimal>> OtherWeightsAsync(int courseId, int termId, int? excludeId, CancellationToken ct);
    Task<bool> ComponentNameExistsAsync(int courseId, int termId, string name, int? excludeId, CancellationToken ct);
    Task<int> CountScoresAsync(int componentId, CancellationToken ct);
    Task<decimal?> HighestScoreAsync(int componentId, CancellationToken ct);

    /// <summary>True when a section of the course in the term already has completed (finalized) enrollments.</summary>
    Task<bool> IsFinalizedAsync(int courseId, int termId, CancellationToken ct);

    void Add(GradeComponent component);
    void Remove(GradeComponent component);

    // ---- Grade entry (instructor) ----
    Task<GradingSectionInfo?> GetSectionAsync(int sectionId, CancellationToken ct);
    Task<List<TeachingSectionRow>> ListTeachingSectionsAsync(int instructorId, CancellationToken ct);
    Task<List<ComponentInfo>> ComponentsAsync(int courseId, int termId, CancellationToken ct);

    /// <summary>Enrolled and Completed students of the section, by name. Deactivated students stay on it (their grades are kept).</summary>
    Task<List<RosterRow>> RosterAsync(int sectionId, CancellationToken ct);

    /// <summary>Tracked scores of these enrollments, optionally for one component.</summary>
    Task<List<StudentGrade>> GradesAsync(IReadOnlyCollection<int> enrollmentIds, int? componentId, CancellationToken ct);

    /// <summary>The grade scale, highest band first.</summary>
    Task<List<GradeBand>> GradeScaleAsync(CancellationToken ct);

    void Add(StudentGrade grade);
    void Remove(StudentGrade grade);

    // ---- Term results (rule G6) ----

    /// <summary>Completed and DroppedAbsence attempts of the student that have grade points, oldest row first.</summary>
    Task<List<AttemptRow>> GradedAttemptsAsync(int studentId, CancellationToken ct);

    /// <summary>Tracked.</summary>
    Task<List<TermResult>> TermResultsAsync(int studentId, CancellationToken ct);

    void Add(TermResult result);
    void Remove(TermResult result);

    // ---- Student views (PB-09) ----
    Task<(int Id, string Name)?> GetCurrentTermAsync(CancellationToken ct);

    /// <summary>The student's Enrolled, Completed and DroppedAbsence enrollments, optionally in one term, by course code.</summary>
    Task<List<StudentCourseRow>> StudentCoursesAsync(int studentId, int? termId, CancellationToken ct);

    Task<List<TermComponentRow>> TermComponentsAsync(int termId, IReadOnlyCollection<int> courseIds, CancellationToken ct);

    /// <summary>Untracked (EnrollmentId, ComponentId, Score) of these enrollments.</summary>
    Task<List<(int EnrollmentId, int ComponentId, decimal Score)>> ScoresAsync(IReadOnlyCollection<int> enrollmentIds, CancellationToken ct);

    /// <summary>Untracked.</summary>
    Task<List<TermResult>> ReadTermResultsAsync(int studentId, CancellationToken ct);
}
