using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Attendance.Models.Entities;

public class AttendanceRecord : BaseEntity
{
    public int SessionId { get; set; }
    public AttendanceSession Session { get; set; } = null!;
    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public AttendanceStatus Status { get; set; }
}
