using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Rules;
using SmartSis.Api.Features.Attendance.Services;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Attendance.AttendanceTestData;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>
/// PB-10 / T39, T42: taking attendance. Uses the seeded CSC 101 A (Mon + Wed 08:00, three classmates enrolled).
/// Each test starts with no attendance at all.
/// </summary>
public sealed class AttendanceApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private async Task<(HttpClient Client, int MeetingId, DateOnly Date)> InstructorOnCsc101Async(int weeksAgo = 0)
    {
        await factory.ClearAttendanceAsync();
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var (meetingId, day) = await factory.MeetingAsync("CSC 101");
        return (client, meetingId, PastDate(day, weeksAgo));
    }

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<Dictionary<string, object>>())?.GetValueOrDefault("code")?.ToString();

    [Fact]
    public async Task Teaching_ListsTaughtSections_WithMeetingsAndTermDates()
    {
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);

        var dto = await (await client.GetAsync("/api/attendance/teaching")).ReadAsync<TeachingSectionsDto>();

        Assert.NotNull(dto.TermId);
        Assert.Equal(Today, dto.Today);
        var csc101 = dto.Sections.Single(s => s.CourseCode == "CSC 101" && s.SectionCode == "A");
        Assert.Equal(9, csc101.AbsenceLimit);
        Assert.Equal([1, 3], csc101.Meetings.Select(m => m.DayOfWeek));
        Assert.True(csc101.TermStart <= Today && Today <= csc101.TermEnd);
        Assert.True(csc101.EnrolledCount >= 3);
    }

    [Fact]
    [Trait("TC", "TC_36")]
    public async Task Sheet_BeforeSubmission_ListsEnrolledStudents_Unmarked()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();

        var sheet = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();

        Assert.Null(sheet.SessionId);
        Assert.Equal("CSC 101", sheet.CourseCode);
        Assert.Contains(sheet.Students, s => s.UniversityId == 3004);
        Assert.DoesNotContain(sheet.Students, s => s.UniversityId == SisApiFactory.StudentId); // 3001 isn't in CSC 101 A
        Assert.All(sheet.Students, s => Assert.Null(s.Status));
    }

    [Fact]
    [Trait("TC", "TC_37")]
    public async Task Submit_SavesEveryMark_AndTheSheetShowsThem()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var roster = (await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>()).Students;
        var absentId = roster[0].EnrollmentId;

        var res = await client.SubmitAsync(meetingId, date, absentId);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var result = await res.ReadAsync<SubmitAttendanceResultDto>();
        Assert.Equal(1, result.Absent);
        Assert.Equal(roster.Count - 1, result.Present);

        var sheet = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        Assert.Equal(result.SessionId, sheet.SessionId);
        Assert.NotNull(sheet.SubmittedAt);
        Assert.Equal(AttendanceStatus.Absent, sheet.Students.Single(s => s.EnrollmentId == absentId).Status);
        Assert.Equal(1, sheet.Students.Single(s => s.EnrollmentId == absentId).Absences);
        Assert.All(sheet.Students.Where(s => s.EnrollmentId != absentId), s => Assert.Equal(AttendanceStatus.Present, s.Status));

        var byId = await (await client.GetAsync($"/api/attendance/sessions/{result.SessionId}")).ReadAsync<AttendanceSheetDto>();
        Assert.Equal(sheet.Students, byId.Students);
    }

    [Fact]
    [Trait("TC", "TC_38")]
    [Trait("Rule", "A4")]
    public async Task Submit_SameMeetingAndDateTwice_Returns409_AndKeepsTheFirst()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var first = await client.SubmitAsync(meetingId, date);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var sheet = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        var marks = sheet.Students.Select(s => new AttendanceMarkRequest(s.EnrollmentId, AttendanceStatus.Absent)).ToList();
        var second = await client.PostAsJsonAsync("/api/attendance/sessions", new SubmitAttendanceRequest(meetingId, date, marks), TestData.Json);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(AttendanceService.AlreadyTaken, await CodeAsync(second));
        var after = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        Assert.All(after.Students, s => Assert.Equal(AttendanceStatus.Present, s.Status));
    }

    [Fact]
    public async Task Submit_SameDate_OtherMeeting_IsAllowed()
    {
        var (client, mondayMeeting, monday) = await InstructorOnCsc101Async();
        Assert.Equal(HttpStatusCode.Created, (await client.SubmitAsync(mondayMeeting, monday)).StatusCode);

        // MAT 201 A also meets on Monday: a different meeting on the same date is its own session.
        var (otherMeeting, _) = await factory.MeetingAsync("MAT 201");
        Assert.Equal(HttpStatusCode.Created, (await client.SubmitAsync(otherMeeting, monday)).StatusCode);
    }

    [Fact]
    public async Task Sheet_FutureDate_Returns409()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var res = await client.GetSheetAsync(meetingId, date.AddDays(14));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(SessionDatePolicy.FutureDate, await CodeAsync(res));
    }

    [Fact]
    public async Task Submit_WrongWeekday_Returns409()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var res = await client.PostAsJsonAsync("/api/attendance/sessions",
            new SubmitAttendanceRequest(meetingId, date.AddDays(-1), [new AttendanceMarkRequest(1, AttendanceStatus.Present)]), TestData.Json);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(SessionDatePolicy.WrongDay, await CodeAsync(res));
    }

    [Fact]
    public async Task Submit_BeforeTheTermStarted_Returns409()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var res = await client.GetSheetAsync(meetingId, date.AddDays(-7 * 10));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(SessionDatePolicy.OutsideTerm, await CodeAsync(res));
    }

    [Fact]
    public async Task Submit_StudentLeftOut_Returns409()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var sheet = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        var marks = sheet.Students.Skip(1).Select(s => new AttendanceMarkRequest(s.EnrollmentId, AttendanceStatus.Present)).ToList();

        var res = await client.PostAsJsonAsync("/api/attendance/sessions", new SubmitAttendanceRequest(meetingId, date, marks), TestData.Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(AttendanceService.MissingStudents, await CodeAsync(res));
    }

    [Fact]
    public async Task Submit_StudentOfAnotherSection_Returns409()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var sheet = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        var (mat201Meeting, matDay) = await factory.MeetingAsync("MAT 201");
        var outsider = (await (await client.GetSheetAsync(mat201Meeting, PastDate(matDay))).ReadAsync<AttendanceSheetDto>())
            .Students.First(s => sheet.Students.All(x => x.EnrollmentId != s.EnrollmentId));
        var marks = sheet.Students.Select(s => new AttendanceMarkRequest(s.EnrollmentId, AttendanceStatus.Present))
            .Append(new AttendanceMarkRequest(outsider.EnrollmentId, AttendanceStatus.Absent)).ToList();

        var res = await client.PostAsJsonAsync("/api/attendance/sessions", new SubmitAttendanceRequest(meetingId, date, marks), TestData.Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal(AttendanceService.NotOnRoster, await CodeAsync(res));
    }

    [Fact]
    public async Task Submit_NoMarks_Returns400()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var res = await client.PostAsJsonAsync("/api/attendance/sessions", new SubmitAttendanceRequest(meetingId, date, []), TestData.Json);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("records", problem.Errors.Keys);
    }

    [Fact]
    public async Task Submit_SameStudentTwice_Returns400()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var res = await client.PostAsJsonAsync("/api/attendance/sessions", new SubmitAttendanceRequest(meetingId, date,
            [new AttendanceMarkRequest(5, AttendanceStatus.Present), new AttendanceMarkRequest(5, AttendanceStatus.Absent)]), TestData.Json);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Sheet_UnknownMeeting_Returns404()
    {
        var (client, _, date) = await InstructorOnCsc101Async();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetSheetAsync(999_999, date)).StatusCode);
    }

    [Fact]
    [Trait("TC", "TC_43")]
    [Trait("SR", "SR-22")]
    public async Task AnotherInstructorsSection_Returns403()
    {
        var (client, _, _) = await InstructorOnCsc101Async();
        var (sectionId, meetingId) = await factory.OtherInstructorsSectionAsync();
        var monday = PastDate(1);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetSheetAsync(meetingId, monday)).StatusCode);
        var submit = await client.PostAsJsonAsync("/api/attendance/sessions",
            new SubmitAttendanceRequest(meetingId, monday, [new AttendanceMarkRequest(1, AttendanceStatus.Present)]), TestData.Json);
        Assert.Equal(HttpStatusCode.Forbidden, submit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/attendance/sections/{sectionId}/sessions")).StatusCode);

        var teaching = await (await client.GetAsync("/api/attendance/teaching")).ReadAsync<TeachingSectionsDto>();
        Assert.DoesNotContain(teaching.Sections, s => s.SectionId == sectionId);
    }

    [Fact]
    public async Task Sessions_ListsSubmittedSessions_NewestFirst_WithCounts()
    {
        var (client, meetingId, date) = await InstructorOnCsc101Async();
        var sheet = await (await client.GetSheetAsync(meetingId, date)).ReadAsync<AttendanceSheetDto>();
        await client.SubmitAsync(meetingId, date.AddDays(-7));
        await client.SubmitAsync(meetingId, date, sheet.Students[0].EnrollmentId);

        var sessions = await (await client.GetAsync($"/api/attendance/sections/{sheet.SectionId}/sessions")).ReadAsync<List<SessionSummaryDto>>();

        Assert.Equal([date, date.AddDays(-7)], sessions.Select(s => s.SessionDate));
        Assert.Equal(1, sessions[0].Absent);
        Assert.Equal(sheet.Students.Count - 1, sessions[0].Present);
        Assert.Equal(0, sessions[1].Absent);
    }
}
