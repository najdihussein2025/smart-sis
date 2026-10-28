using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Enrollments.Models.Entities;

/// <summary>One attempt of a student in a section. Retakes create a new row (rule G5).</summary>
public class Enrollment : BaseEntity
{
    /// <summary>Students.UserId.</summary>
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled;
    public decimal? FinalPct { get; set; }
    public string? LetterGrade { get; set; }
    public decimal? GradePoints { get; set; }
    public bool CountsInGpa { get; set; } = true;
    public DateTime EnrolledAt { get; set; }
    public DateTime? DroppedAt { get; set; }

    public bool IsActive => Status == EnrollmentStatus.Enrolled;

    /// <summary>Rule E3: a completed attempt with D or better.</summary>
    public bool HasPassed => Status == EnrollmentStatus.Completed && GradePoints >= 1.0m;

    public void Drop(DateTime utcNow)
    {
        EnsureActive();
        Status = EnrollmentStatus.Dropped;
        CountsInGpa = false;
        DroppedAt = utcNow;
    }

    /// <summary>Rule A3: absence drop records an F that counts in the GPA.</summary>
    public void DropForAbsence(DateTime utcNow)
    {
        EnsureActive();
        Status = EnrollmentStatus.DroppedAbsence;
        LetterGrade = "F";
        GradePoints = 0m;
        CountsInGpa = true;
        DroppedAt = utcNow;
    }

    public void Complete(decimal finalPct, string letter, decimal gradePoints)
    {
        EnsureActive();
        Status = EnrollmentStatus.Completed;
        FinalPct = finalPct;
        LetterGrade = letter;
        GradePoints = gradePoints;
    }

    /// <summary>Rule G5: an older attempt stops counting once a newer one completes.</summary>
    public void Supersede() => CountsInGpa = false;

    private void EnsureActive()
    {
        if (Status != EnrollmentStatus.Enrolled)
            throw new BusinessRuleException($"This enrollment is already {Status}.");
    }
}
