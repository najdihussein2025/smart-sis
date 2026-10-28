using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Enrollments.Models.Entities;

/// <summary>Audit log of every enroll / swap / drop / auto-drop (rule E9).</summary>
public class EnrollmentAction : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public EnrollmentActionType Action { get; set; }
    public int? FromSectionId { get; set; }
    public int? ToSectionId { get; set; }

    /// <summary>Users.Id of who did it (the student, an admin, or the system user for auto-drop).</summary>
    public int PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
}
