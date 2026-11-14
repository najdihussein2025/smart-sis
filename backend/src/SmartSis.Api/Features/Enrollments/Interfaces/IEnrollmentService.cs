using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Enrollments.Rules;

namespace SmartSis.Api.Features.Enrollments.Interfaces;

/// <summary>Enroll, swap and drop (PB-05, PB-07). The student always comes from the token.</summary>
public interface IEnrollmentService
{
    Task<MyEnrollmentsDto> GetMineAsync(CancellationToken ct);
    Task<EnrollResultDto> EnrollAsync(EnrollRequest request, CancellationToken ct);
    Task<MyEnrollmentsDto> SwapAsync(SwapRequest request, CancellationToken ct);
    Task DropAsync(DropRequest request, CancellationToken ct);

    /// <summary>
    /// Rule A3, called by Attendance at limit + 1: Status = DroppedAbsence, F, 0.0, counts in GPA, plus an AutoDrop action.
    /// Does not save: the caller's unit of work commits it together with the attendance that caused it.
    /// </summary>
    Task DropForAbsenceAsync(int enrollmentId, int performedBy, CancellationToken ct);

    /// <summary>
    /// Called by Grading when an instructor finalizes a section: Status = Completed with the final %, letter and points, and
    /// rule G5: older graded attempts of the same course stop counting in the GPA (CountsInGpa = false).
    /// Does not save: the caller's unit of work commits it with the rest of the finalization.
    /// </summary>
    Task CompleteAsync(int enrollmentId, decimal finalPct, string letterGrade, decimal gradePoints, CancellationToken ct);
}

/// <summary>The current term's dates.</summary>
public sealed record CurrentTerm(int Id, string Name, DateTime EnrollStart, DateTime EnrollEnd, DateTime AddDropStart, DateTime AddDropEnd);

/// <summary>Facts about a section, read after its row lock.</summary>
public sealed record LockedSection(int Id, int TermId, SectionCandidate Candidate);

public interface IEnrollmentRepository
{
    Task<CurrentTerm?> GetCurrentTermAsync(CancellationToken ct);
    Task<int> GetMaxCreditsAsync(int studentId, CancellationToken ct);

    /// <summary>Sections the student is Enrolled in for the term.</summary>
    Task<List<HeldSection>> GetHeldSectionsAsync(int studentId, int termId, CancellationToken ct);

    /// <summary>Courses passed with D or better (Completed, GradePoints ≥ 1.0 — rule E3).</summary>
    Task<HashSet<int>> GetPassedCourseIdsAsync(int studentId, CancellationToken ct);

    /// <summary>Takes an UPDLOCK, ROWLOCK on the section row (SQL Server) and reads it with a fresh seat count (rule E6).</summary>
    Task<LockedSection?> LockSectionAsync(int sectionId, CancellationToken ct);

    Task<List<EnrolledSectionDto>> ListMineAsync(int studentId, int termId, CancellationToken ct);

    /// <summary>The student's Enrolled row for a section, tracked.</summary>
    Task<Enrollment?> FindActiveAsync(int studentId, int sectionId, CancellationToken ct);

    Task<Enrollment?> FindAsync(int enrollmentId, CancellationToken ct);

    Task<int> GetCourseIdAsync(int sectionId, CancellationToken ct);

    /// <summary>The student's other graded attempts (Completed or DroppedAbsence) of a course that still count in the GPA, tracked.</summary>
    Task<List<Enrollment>> FindCountingAttemptsAsync(int studentId, int courseId, int exceptEnrollmentId, CancellationToken ct);

    void Add(Enrollment enrollment);
    void Add(EnrollmentAction action);
}
