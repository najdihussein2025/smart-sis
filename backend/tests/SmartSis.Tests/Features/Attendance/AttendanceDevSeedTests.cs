using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Attendance.AttendanceTestData;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>The simulated demo attendance (AttendanceDevSeed): Demo Student sees a Warning in MAT 201 and Perfect in CSC 210.</summary>
public sealed class AttendanceDevSeedTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    [Fact]
    public async Task DemoStudent_HasWarningInMat201_PerfectInCsc210()
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);

        var mine = await (await client.GetAsync("/api/attendance/my")).ReadAsync<MyAttendanceDto>();

        var mat = mine.Courses.Single(c => c.CourseCode == "MAT 201");
        Assert.Equal((3, AbsenceStanding.Warning), (mat.Absences, mat.Standing));
        Assert.True(mat.SessionsRecorded >= 5);
        var csc = mine.Courses.Single(c => c.CourseCode == "CSC 210");
        Assert.Equal((0, AbsenceStanding.Perfect), (csc.Absences, csc.Standing));
        Assert.True(csc.SessionsRecorded > 0);
    }

    [Fact]
    public async Task LastWeek_IsLeftOpen_ForTheInstructor()
    {
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var (meetingId, day) = await factory.MeetingAsync("MAT 201");

        var sheet = await (await client.GetSheetAsync(meetingId, PastDate(day))).ReadAsync<AttendanceSheetDto>();

        Assert.Null(sheet.SessionId);
    }
}
