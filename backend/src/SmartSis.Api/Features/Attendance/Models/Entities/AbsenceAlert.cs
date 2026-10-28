using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Attendance.Models.Entities;

/// <summary>Proof that an alert was sent, so each level is sent once per enrollment (rule A2).</summary>
public class AbsenceAlert : BaseEntity
{
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public AbsenceAlertLevel Level { get; set; }
    public int AbsenceCount { get; set; }
    public DateTime SentAt { get; set; }
}
