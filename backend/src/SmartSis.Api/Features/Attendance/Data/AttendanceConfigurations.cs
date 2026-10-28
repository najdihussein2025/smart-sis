using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Attendance.Data;

internal sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> b)
    {
        b.ToTable("AttendanceSessions");
        b.HasIndex(x => new { x.MeetingId, x.SessionDate }).IsUnique(); // rule A4
        b.HasIndex(x => x.SectionId);
        b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId);
        b.HasOne(x => x.Meeting).WithMany().HasForeignKey(x => x.MeetingId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SubmittedBy);
    }
}

internal sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.ToTable("AttendanceRecords");
        b.HasIndex(x => new { x.SessionId, x.EnrollmentId }).IsUnique();
        b.HasIndex(x => new { x.EnrollmentId, x.Status });
        b.HasOne(x => x.Session).WithMany(s => s.Records).HasForeignKey(x => x.SessionId);
        b.HasOne(x => x.Enrollment).WithMany().HasForeignKey(x => x.EnrollmentId);
    }
}

internal sealed class AbsenceAlertConfiguration : IEntityTypeConfiguration<AbsenceAlert>
{
    public void Configure(EntityTypeBuilder<AbsenceAlert> b)
    {
        b.ToTable("AbsenceAlerts");
        b.HasIndex(x => new { x.EnrollmentId, x.Level }).IsUnique();
        b.HasOne(x => x.Enrollment).WithMany().HasForeignKey(x => x.EnrollmentId);
    }
}
