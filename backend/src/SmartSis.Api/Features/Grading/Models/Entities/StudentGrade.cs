using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Grading.Models.Entities;

/// <summary>One score per enrollment per component (rule G2).</summary>
public class StudentGrade : BaseEntity
{
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public int ComponentId { get; set; }
    public GradeComponent Component { get; set; } = null!;
    public decimal Score { get; set; }

    /// <summary>Users.Id of the instructor.</summary>
    public int GradedBy { get; set; }
    public DateTime GradedAt { get; set; }
}
