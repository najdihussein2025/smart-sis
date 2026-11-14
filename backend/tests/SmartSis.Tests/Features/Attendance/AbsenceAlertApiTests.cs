using System.Net;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Attendance.AttendanceTestData;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>
/// T40 / T42: what a submission does once a student's absences reach limit − 1, the limit and limit + 1 (CSC 101: 3 credits,
/// limit 9). Each test uses its own simulated student in CSC 101 A, with earlier absences put straight in the database.
/// </summary>
public sealed class AbsenceAlertApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private async Task<(HttpClient Client, int MeetingId, DateOnly Date, int EnrollmentId)> StudentWithAbsencesAsync(long universityId, int earlier)
    {
        await factory.ClearAttendanceAsync();
        var (meetingId, day) = await factory.MeetingAsync("CSC 101");
        var enrollmentId = await factory.EnrollDirectlyAsync(universityId, await factory.SectionIdAsync("CSC 101"));
        await factory.AddPastAbsencesAsync(enrollmentId, meetingId, earlier);
        return (await factory.ClientForAsync(SisApiFactory.InstructorId), meetingId, PastDate(day), enrollmentId);
    }

    private async Task<(List<AbsenceAlert> Alerts, List<EmailOutboxMessage> Emails, List<Notification> Notes, Enrollment Enrollment)> StateAsync(
        long universityId, int enrollmentId)
    {
        var userId = await factory.UserIdAsync(universityId);
        (List<AbsenceAlert>, List<EmailOutboxMessage>, List<Notification>, Enrollment) result = default;
        await factory.WithDbAsync(async db => result = (
            await db.Set<AbsenceAlert>().Where(a => a.EnrollmentId == enrollmentId).ToListAsync(),
            await db.Set<EmailOutboxMessage>().Where(m => m.ToEmail == $"student{universityId}@smartsis.local").ToListAsync(),
            await db.Set<Notification>().Where(n => n.UserId == userId).ToListAsync(),
            await db.Set<Enrollment>().SingleAsync(e => e.Id == enrollmentId)));
        return result;
    }

    [Fact]
    [Trait("TC", "TC_39")]
    [Trait("Rule", "A2")]
    public async Task EighthAbsence_SendsWarning_EmailAndNotification_Once()
    {
        const long uid = 3911;
        var (client, meetingId, date, enrollmentId) = await StudentWithAbsencesAsync(uid, earlier: 7);

        var res = await client.SubmitAsync(meetingId, date, enrollmentId);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var alert = Assert.Single((await res.ReadAsync<SubmitAttendanceResultDto>()).Alerts);
        Assert.Equal((enrollmentId, AbsenceAlertLevel.Warning, 8, 9), (alert.EnrollmentId, alert.Level, alert.Absences, alert.Limit));

        var (alerts, emails, notes, enrollment) = await StateAsync(uid, enrollmentId);
        Assert.Equal(AbsenceAlertLevel.Warning, Assert.Single(alerts).Level);
        Assert.Equal("Attendance warning: CSC 101", Assert.Single(emails).Subject);
        Assert.Equal(NotificationType.AbsenceWarning, Assert.Single(notes).Type);
        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
    }

    [Fact]
    [Trait("Rule", "A2")]
    public async Task PresentAfterWarning_SendsNothingMore()
    {
        const long uid = 3912;
        var (client, meetingId, date, enrollmentId) = await StudentWithAbsencesAsync(uid, earlier: 7);
        await client.SubmitAsync(meetingId, date, enrollmentId);

        var res = await client.SubmitAsync(meetingId, date.AddDays(-7)); // present this time

        Assert.Empty((await res.ReadAsync<SubmitAttendanceResultDto>()).Alerts);
        var (alerts, emails, notes, _) = await StateAsync(uid, enrollmentId);
        Assert.Single(alerts);
        Assert.Single(emails);
        Assert.Single(notes);
    }

    [Fact]
    [Trait("TC", "TC_40")]
    [Trait("Rule", "A2")]
    public async Task NinthAbsence_SendsLimitAlert_StillEnrolled()
    {
        const long uid = 3913;
        var (client, meetingId, date, enrollmentId) = await StudentWithAbsencesAsync(uid, earlier: 7);
        await client.SubmitAsync(meetingId, date.AddDays(-7), enrollmentId); // 8th → Warning

        var res = await client.SubmitAsync(meetingId, date, enrollmentId);   // 9th → Limit

        Assert.Equal(AbsenceAlertLevel.Limit, Assert.Single((await res.ReadAsync<SubmitAttendanceResultDto>()).Alerts).Level);
        var (alerts, emails, notes, enrollment) = await StateAsync(uid, enrollmentId);
        Assert.Equal([AbsenceAlertLevel.Warning, AbsenceAlertLevel.Limit], alerts.OrderBy(a => a.Level).Select(a => a.Level));
        Assert.Equal(2, emails.Count);
        Assert.Contains(notes, n => n.Type == NotificationType.AbsenceLimit && n.Link == "/student/attendance");
        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
    }

    [Fact]
    [Trait("TC", "TC_41")]
    [Trait("Rule", "A3")]
    public async Task TenthAbsence_AutoDrops_WithFCountingInGpa_AndLeavesTheRoster()
    {
        const long uid = 3914;
        var (client, meetingId, date, enrollmentId) = await StudentWithAbsencesAsync(uid, earlier: 9);

        var res = await client.SubmitAsync(meetingId, date, enrollmentId);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var alert = Assert.Single((await res.ReadAsync<SubmitAttendanceResultDto>()).Alerts);
        Assert.Equal((AbsenceAlertLevel.AutoDropped, 10), (alert.Level, alert.Absences));

        var (alerts, emails, notes, enrollment) = await StateAsync(uid, enrollmentId);
        Assert.Equal(EnrollmentStatus.DroppedAbsence, enrollment.Status);
        Assert.Equal("F", enrollment.LetterGrade);
        Assert.Equal(0m, enrollment.GradePoints);
        Assert.True(enrollment.CountsInGpa);
        Assert.NotNull(enrollment.DroppedAt);
        Assert.Contains(alerts, a => a.Level == AbsenceAlertLevel.AutoDropped);
        Assert.Contains(emails, e => e.Subject == "Dropped from CSC 101 for absences");
        Assert.Contains(notes, n => n.Type == NotificationType.AutoDropped);

        var instructorUserId = await factory.UserIdAsync(SisApiFactory.InstructorId);
        await factory.WithDbAsync(async db =>
        {
            var action = await db.Set<EnrollmentAction>().SingleAsync(a => a.Action == EnrollmentActionType.AutoDrop && a.FromSectionId == enrollment.SectionId && a.StudentId == enrollment.StudentId);
            Assert.Equal(instructorUserId, action.PerformedBy); // rule E9
        });

        // The dropped student is no longer on the next roster.
        var next = await (await client.GetSheetAsync(meetingId, date.AddDays(-7))).ReadAsync<AttendanceSheetDto>();
        Assert.DoesNotContain(next.Students, s => s.EnrollmentId == enrollmentId);
        // The session that dropped them still shows their absence.
        var taken = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        Assert.Equal(AttendanceStatus.Absent, taken.Students.Single(s => s.EnrollmentId == enrollmentId).Status);
    }

    [Fact]
    public async Task OtherStudentsInTheSession_AreNotAffected()
    {
        const long uid = 3915;
        var (client, meetingId, date, enrollmentId) = await StudentWithAbsencesAsync(uid, earlier: 9);

        var res = await client.SubmitAsync(meetingId, date, enrollmentId);

        var result = await res.ReadAsync<SubmitAttendanceResultDto>();
        Assert.All(result.Alerts, a => Assert.Equal(enrollmentId, a.EnrollmentId));
        var classmate = await factory.UserIdAsync(3004);
        await factory.WithDbAsync(async db =>
            Assert.Equal(EnrollmentStatus.Enrolled, (await db.Set<Enrollment>().SingleAsync(e => e.StudentId == classmate && e.Section.Course.Code == "CSC 101" && e.Section.Term.IsCurrent)).Status));
    }
}
