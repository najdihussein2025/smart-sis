using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Attendance.AttendanceTestData;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>T42: the database keeps attendance consistent (rule A4) and a refused submission leaves nothing behind.</summary>
public sealed class AttendanceIntegrityTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    [Fact]
    [Trait("Rule", "A4")]
    [Trait("TC", "TC_38")]
    public async Task Model_SessionsAreUniquePerMeetingAndDate()
    {
        await factory.WithDbAsync(db =>
        {
            var index = db.Model.FindEntityType(typeof(AttendanceSession))!.GetIndexes()
                .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(AttendanceSession.MeetingId), nameof(AttendanceSession.SessionDate)]));
            Assert.True(index.IsUnique);

            var alerts = db.Model.FindEntityType(typeof(AbsenceAlert))!.GetIndexes()
                .Single(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(AbsenceAlert.EnrollmentId), nameof(AbsenceAlert.Level)]));
            Assert.True(alerts.IsUnique); // rule A2: each level once per enrollment
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task RefusedSubmission_SavesNothing()
    {
        await factory.ClearAttendanceAsync();
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var (meetingId, day) = await factory.MeetingAsync("CSC 101");
        var date = PastDate(day);
        var sheet = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        var partial = sheet.Students.Skip(1).Select(s => new AttendanceMarkRequest(s.EnrollmentId, AttendanceStatus.Absent)).ToList();

        var res = await client.PostAsJsonAsync("/api/attendance/sessions", new SubmitAttendanceRequest(meetingId, date, partial), TestData.Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        await factory.WithDbAsync(async db =>
        {
            Assert.False(await db.Set<AttendanceSession>().AnyAsync());
            Assert.False(await db.Set<AttendanceRecord>().AnyAsync());
        });
        Assert.Null((await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>()).SessionId);
    }

    [Fact]
    [Trait("TC", "TC_41")]
    [Trait("Rule", "A3")]
    public async Task FourMoreAbsences_ThroughTheApi_WarnOnce_AlertOnce_ThenDrop()
    {
        await factory.ClearAttendanceAsync();
        var enrollmentId = await factory.EnrollDirectlyAsync(3921, await factory.SectionIdAsync("CSC 101"));
        await factory.AddPastAbsencesAsync(enrollmentId, (await factory.MeetingAsync("CSC 101")).MeetingId, 6);
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var sectionId = await factory.SectionIdAsync("CSC 101");
        var meetings = new List<(int Id, int Day)>();
        await factory.WithDbAsync(async db => meetings = (await db.Set<SmartSis.Api.Features.Sections.Models.Entities.SectionMeeting>()
            .Where(m => m.SectionId == sectionId).OrderBy(m => m.DayOfWeek).ToListAsync()).Select(m => (m.Id, m.DayOfWeek)).ToList());

        // Absences 7 to 10: Monday and Wednesday of the last two weeks, oldest first.
        var dates = Enumerable.Range(0, 2).SelectMany(w => meetings.Select(m => (m.Id, Date: PastDate(m.Day, w))))
            .OrderBy(x => x.Date).ToList();
        Assert.Equal(4, dates.Count);
        var levels = new List<AbsenceAlertLevel>();
        foreach (var (meetingId, date) in dates)
        {
            var res = await client.SubmitAsync(meetingId, date, enrollmentId);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            levels.AddRange((await res.ReadAsync<SubmitAttendanceResultDto>()).Alerts.Select(a => a.Level));
        }

        Assert.Equal([AbsenceAlertLevel.Warning, AbsenceAlertLevel.Limit, AbsenceAlertLevel.AutoDropped], levels);
        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(EnrollmentStatus.DroppedAbsence, (await db.Set<Enrollment>().SingleAsync(e => e.Id == enrollmentId)).Status);
            Assert.Equal(3, await db.Set<EmailOutboxMessage>().CountAsync(m => m.ToEmail == "student3921@smartsis.local"));
        });
    }
}
