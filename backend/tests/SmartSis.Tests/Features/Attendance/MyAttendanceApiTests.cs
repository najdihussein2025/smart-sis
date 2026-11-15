using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Attendance.AttendanceTestData;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>
/// PB-11 / T43, T45: a student's own attendance. Demo Student (3001) takes CSC 210 A and MAT 201 A (Mon + Wed 10:00).
/// Each test starts with no attendance.
/// </summary>
public sealed class MyAttendanceApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private async Task<(HttpClient Instructor, int MeetingId, int Day, int EnrollmentId)> Mat201Async(long universityId = SisApiFactory.StudentId)
    {
        await factory.ClearAttendanceAsync();
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var (meetingId, day) = await factory.MeetingAsync("MAT 201");
        var sheet = await (await instructor.GetSheetAsync(meetingId, PastDate(day))).ReadAsync<AttendanceSheetDto>();
        return (instructor, meetingId, day, sheet.Students.Single(s => s.UniversityId == universityId).EnrollmentId);
    }

    private async Task<MyAttendanceDto> MineAsync(long universityId = SisApiFactory.StudentId) =>
        await (await (await factory.ClientForAsync(universityId)).GetAsync("/api/attendance/my")).ReadAsync<MyAttendanceDto>();

    [Fact]
    [Trait("Rule", "A5")]
    public async Task NoAbsences_Perfect()
    {
        var (instructor, meetingId, day, _) = await Mat201Async();
        await instructor.SubmitAsync(meetingId, PastDate(day));

        var mine = await MineAsync();

        Assert.NotNull(mine.TermId);
        Assert.Equal(["CSC 210", "MAT 201"], mine.Courses.Select(c => c.CourseCode));
        var mat = mine.Courses.Single(c => c.CourseCode == "MAT 201");
        Assert.Equal((0, 9, 9, AbsenceStanding.Perfect, 1), (mat.Absences, mat.Limit, mat.Remaining, mat.Standing, mat.SessionsRecorded));
        Assert.Empty(mat.AbsenceDates);
        Assert.Equal(0, mine.Courses.Single(c => c.CourseCode == "CSC 210").SessionsRecorded);
    }

    [Fact]
    [Trait("Rule", "A5")]
    public async Task SomeAbsences_Warning_WithTheDates_NewestFirst()
    {
        var (instructor, meetingId, day, enrollmentId) = await Mat201Async();
        var older = PastDate(day, weeksAgo: 2);
        var newer = PastDate(day, weeksAgo: 1);
        await instructor.SubmitAsync(meetingId, older, enrollmentId);
        await instructor.SubmitAsync(meetingId, newer, enrollmentId);
        await instructor.SubmitAsync(meetingId, PastDate(day));

        var mat = (await MineAsync()).Courses.Single(c => c.CourseCode == "MAT 201");

        Assert.Equal((2, 7, AbsenceStanding.Warning, 3), (mat.Absences, mat.Remaining, mat.Standing, mat.SessionsRecorded));
        Assert.Equal([newer, older], mat.AbsenceDates.Select(d => d.Date));
        Assert.All(mat.AbsenceDates, d => Assert.Equal(new TimeOnly(10, 0), d.StartTime));
        Assert.Equal(EnrollmentStatus.Enrolled, mat.EnrollmentStatus);
    }

    [Fact]
    [Trait("Rule", "A5")]
    public async Task AtLimitMinusOne_Critical()
    {
        var (instructor, meetingId, day, enrollmentId) = await Mat201Async();
        await factory.AddPastAbsencesAsync(enrollmentId, meetingId, 7);
        await instructor.SubmitAsync(meetingId, PastDate(day), enrollmentId);

        var mat = (await MineAsync()).Courses.Single(c => c.CourseCode == "MAT 201");

        Assert.Equal((8, 1, AbsenceStanding.Critical), (mat.Absences, mat.Remaining, mat.Standing));
    }

    [Fact]
    [Trait("Rule", "A3")]
    public async Task DroppedForAbsences_StaysListed_AsDroppedAbsence()
    {
        var (instructor, meetingId, day, enrollmentId) = await Mat201Async(3002);
        await factory.AddPastAbsencesAsync(enrollmentId, meetingId, 9);
        await instructor.SubmitAsync(meetingId, PastDate(day), enrollmentId);

        var mat = (await MineAsync(3002)).Courses.Single(c => c.CourseCode == "MAT 201");

        Assert.Equal((10, 0, AbsenceStanding.Critical, EnrollmentStatus.DroppedAbsence), (mat.Absences, mat.Remaining, mat.Standing, mat.EnrollmentStatus));
    }

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task OnlyTheStudentsOwnAbsences()
    {
        var (instructor, meetingId, day, _) = await Mat201Async();
        var sheet = await (await instructor.GetSheetAsync(meetingId, PastDate(day))).ReadAsync<AttendanceSheetDto>();
        var classmate = sheet.Students.First(s => s.UniversityId != SisApiFactory.StudentId).EnrollmentId;
        await instructor.SubmitAsync(meetingId, PastDate(day), classmate);

        var mine = await MineAsync();

        Assert.Equal(0, mine.Courses.Single(c => c.CourseCode == "MAT 201").Absences);
        Assert.Equal(2, mine.Courses.Count);
    }

    [Fact]
    public async Task NoCurrentTerm_Empty()
    {
        await factory.ClearAttendanceAsync();
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        await factory.WithDbAsync(async db =>
        {
            (await db.Set<Term>().SingleAsync(t => t.IsCurrent)).IsCurrent = false;
            await db.SaveChangesAsync();
        });
        try
        {
            var mine = await (await client.GetAsync("/api/attendance/my")).ReadAsync<MyAttendanceDto>();
            Assert.Null(mine.TermId);
            Assert.Empty(mine.Courses);
        }
        finally
        {
            await factory.WithDbAsync(async db =>
            {
                (await db.Set<Term>().OrderBy(t => t.Id).FirstAsync()).IsCurrent = true;
                await db.SaveChangesAsync();
            });
        }
    }
}
