using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Dashboard.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Finance.Enums;
using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Notifications.Models.Dtos;
using SmartSis.Api.Features.Schedule.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Tests.Features.Attendance;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Dashboard;

/// <summary>PB-22: the role dashboards show the same numbers as the feature pages they link to.</summary>
public sealed class DashboardApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private static int TodayDay => (int)DateTime.UtcNow.DayOfWeek;

    private async Task<HttpClient> StudentEnrolledInAsync(params (string Course, string Section)[] sections)
    {
        await factory.ResetStudentAsync();
        await factory.SetWindowAsync(TermWindowState.Enrollment);
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        var ids = new List<int>();
        foreach (var (course, section) in sections) ids.Add(await factory.SectionIdAsync(course, section));
        if (ids.Count > 0) await client.PostAsJsonAsync("/api/enrollment/enroll", new EnrollRequest([.. ids]));
        return client;
    }

    [Fact]
    public async Task Student_MatchesTheFeaturePages()
    {
        var client = await StudentEnrolledInAsync(("CSC 101", "A"), ("MAT 201", "A"));
        await factory.AddCompletedAsync(SisApiFactory.StudentId, "CSC 310", 3.0m, "B");

        var dash = await (await client.GetAsync("/api/dashboard/student")).ReadAsync<StudentDashboardDto>();

        var mine = await (await client.GetAsync("/api/enrollment/my")).ReadAsync<MyEnrollmentsDto>();
        var grades = await (await client.GetAsync("/api/grades/my")).ReadAsync<MyGradesDto>();
        var schedule = await (await client.GetAsync("/api/schedule/my")).ReadAsync<ScheduleDto>();
        var money = await (await client.GetAsync("/api/finance/my")).ReadAsync<MyFinanceDto>();
        var unread = await (await client.GetAsync("/api/notifications/unread-count")).ReadAsync<UnreadCountDto>();

        Assert.Equal(mine.TermName, dash.TermName);
        Assert.True(mine.EnrolledCredits > 0);
        Assert.Equal(mine.EnrolledCredits, dash.EnrolledCredits);
        Assert.Equal(18, dash.MaxCredits);
        Assert.Equal(3.0m, dash.CumulativeGpa);
        Assert.Equal(grades.CumulativeGpa, dash.CumulativeGpa);
        Assert.Equal(grades.CreditsEarned, dash.CreditsEarned);
        Assert.Equal(schedule.Items.Count(i => i.DayOfWeek == TodayDay), dash.ClassesToday);
        Assert.Equal(money.OutstandingBalance, dash.OutstandingBalance);
        Assert.Equal(money.OverdueBalance, dash.OverdueBalance);
        Assert.Equal(money.NextDueDate, dash.NextDueDate);
        Assert.Equal(unread.Count, dash.UnreadNotifications);
    }

    [Fact]
    public async Task Student_NoGradesYet_GpaIsNull()
    {
        var client = await StudentEnrolledInAsync();
        await factory.WithDbAsync(async db =>
        {
            var userId = await db.Set<User>().Where(u => u.UniversityId == SisApiFactory.StudentId).Select(u => u.Id).SingleAsync();
            Assert.False(await db.Set<Enrollment>().AnyAsync(e => e.StudentId == userId));
        });

        var dash = await (await client.GetAsync("/api/dashboard/student")).ReadAsync<StudentDashboardDto>();

        Assert.Null(dash.CumulativeGpa);
        Assert.Equal(0, dash.EnrolledCredits);
        Assert.Equal(0, dash.ClassesToday);
        Assert.Equal(0, dash.AttendanceWarnings);
    }

    [Fact]
    [Trait("Rule", "A5")]
    public async Task Student_AbsencesInACourse_CountAsAttendanceWarnings()
    {
        var client = await StudentEnrolledInAsync(("CSC 101", "A"), ("MAT 201", "A"));
        await factory.ClearAttendanceAsync();
        var studentUserId = await factory.UserIdAsync(SisApiFactory.StudentId);
        var csc = await factory.SectionIdAsync("CSC 101");
        var mat = await factory.SectionIdAsync("MAT 201");
        int cscEnrollment = 0, matEnrollment = 0;
        await factory.WithDbAsync(async db =>
        {
            cscEnrollment = await db.Set<Enrollment>().Where(e => e.StudentId == studentUserId && e.SectionId == csc).Select(e => e.Id).SingleAsync();
            matEnrollment = await db.Set<Enrollment>().Where(e => e.StudentId == studentUserId && e.SectionId == mat).Select(e => e.Id).SingleAsync();
        });
        var (cscMeeting, _) = await factory.MeetingAsync("CSC 101");
        var (matMeeting, _) = await factory.MeetingAsync("MAT 201");

        // One absence → Warning. Limit − 1 absences → Critical (3 credits → limit 9).
        await factory.AddPastAbsencesAsync(cscEnrollment, cscMeeting, 1);
        await factory.AddPastAbsencesAsync(matEnrollment, matMeeting, 8);

        var dash = await (await client.GetAsync("/api/dashboard/student")).ReadAsync<StudentDashboardDto>();
        var mine = await (await client.GetAsync("/api/attendance/my")).ReadAsync<MyAttendanceDto>();

        Assert.Equal(2, dash.AttendanceWarnings);
        Assert.Equal(1, dash.AttendanceCritical);
        Assert.Equal(mine.Courses.Count(c => c.Standing != SmartSis.Api.Features.Attendance.Enums.AbsenceStanding.Perfect), dash.AttendanceWarnings);
        await factory.ClearAttendanceAsync();
    }

    [Fact]
    public async Task Instructor_MatchesTeachingSectionsAndGrading()
    {
        await factory.ClearAttendanceAsync();
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);

        var dash = await (await client.GetAsync("/api/dashboard/instructor")).ReadAsync<InstructorDashboardDto>();

        var teaching = await (await client.GetAsync("/api/attendance/teaching")).ReadAsync<TeachingSectionsDto>();
        var grading = await (await client.GetAsync("/api/instructor/grading/sections"))
            .ReadAsync<List<SmartSis.Api.Features.Grading.Models.Dtos.TeachingSectionDto>>();

        // The seeded instructor teaches all five seeded sections.
        Assert.Equal(5, dash.ActiveSections);
        Assert.Equal(teaching.TermName, dash.TermName);
        Assert.Equal(teaching.Today, dash.Today);
        Assert.Equal(teaching.Sections.SelectMany(s => s.Meetings).Count(m => m.DayOfWeek == TodayDay), dash.ClassesToday);
        Assert.Equal(grading.Where(s => !s.IsFinalized).Sum(s => s.NotGraded), dash.MissingScores);
        Assert.Equal(grading.Count(s => !s.IsFinalized && s.NotGraded > 0), dash.SectionsWithMissingScores);
    }

    [Fact]
    public async Task Instructor_TakingASessionThisWeek_LowersAttendanceToTake()
    {
        await factory.ClearAttendanceAsync();
        await factory.WithDbAsync(async db =>
        {
            // Make the whole of this week part of the term, so the count doesn't depend on the seeded dates.
            var term = await db.Set<Term>().SingleAsync(t => t.IsCurrent);
            term.StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
            term.EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
            await db.SaveChangesAsync();
        });
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var before = (await (await client.GetAsync("/api/dashboard/instructor")).ReadAsync<InstructorDashboardDto>()).AttendanceToTake;

        // A meeting that has already happened this week, if any (on Monday only Monday's meetings have).
        var today = AttendanceTestData.Today;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        (int Id, int SectionId, DateOnly Date)? due = null;
        await factory.WithDbAsync(async db =>
        {
            var instructorId = await db.Set<User>().Where(u => u.UniversityId == SisApiFactory.InstructorId).Select(u => u.Id).SingleAsync();
            var meetings = await db.Set<SectionMeeting>()
                .Where(m => m.Section.InstructorId == instructorId && m.Section.Term.IsCurrent)
                .Select(m => new { m.Id, m.SectionId, m.DayOfWeek }).ToListAsync();
            for (var d = monday; d <= today && due is null; d = d.AddDays(1))
            {
                var m = meetings.FirstOrDefault(x => x.DayOfWeek == (int)d.DayOfWeek);
                if (m is not null) due = (m.Id, m.SectionId, d);
            }
            var expected = meetings.Sum(m => Enumerable.Range(0, today.DayNumber - monday.DayNumber + 1)
                .Count(i => (int)monday.AddDays(i).DayOfWeek == m.DayOfWeek));
            Assert.Equal(expected, before);
        });

        if (due is not { } taken) return; // today is Monday and nothing meets on Monday: nothing to take yet

        await factory.WithDbAsync(async db =>
        {
            var instructorId = await db.Set<User>().Where(u => u.UniversityId == SisApiFactory.InstructorId).Select(u => u.Id).SingleAsync();
            db.Add(new AttendanceSession
            {
                SectionId = taken.SectionId, MeetingId = taken.Id, SessionDate = taken.Date, SubmittedBy = instructorId, SubmittedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var after = (await (await client.GetAsync("/api/dashboard/instructor")).ReadAsync<InstructorDashboardDto>()).AttendanceToTake;
        Assert.Equal(before - 1, after);
        await factory.ClearAttendanceAsync();
    }

    [Fact]
    public async Task Admin_CountsMatchTheDatabase()
    {
        var client = await factory.ClientForAsync(SisApiFactory.AdminId);
        await factory.AddStudentAsync(3901);

        var dash = await (await client.GetAsync("/api/dashboard/admin")).ReadAsync<AdminDashboardDto>();

        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(await db.Set<User>().CountAsync(u => u.Role == UserRole.Student && u.IsActive), dash.ActiveStudents);
            Assert.Equal(await db.Set<User>().CountAsync(u => u.Role == UserRole.Instructor && u.IsActive), dash.ActiveInstructors);
            Assert.Equal(await db.Set<Section>().CountAsync(s => s.Term.IsCurrent), dash.SectionsThisTerm);
            Assert.Equal(await db.Set<Term>().Where(t => t.IsCurrent).Select(t => t.Name).SingleAsync(), dash.TermName);
            Assert.Equal(await db.Set<SupportRequest>().CountAsync(r => r.Status == SupportStatus.Open || r.Status == SupportStatus.InProgress), dash.OpenSupportRequests);
            Assert.Equal(await db.Set<SupportRequest>().CountAsync(r => r.Status == SupportStatus.Open), dash.NewSupportRequests);

            var unpaid = await db.Set<Charge>().Include(c => c.Payments).Where(c => c.Status != ChargeStatus.Paid).ToListAsync();
            Assert.Equal(unpaid.Count, dash.UnpaidCharges);
            Assert.Equal(unpaid.Sum(c => c.Balance), dash.OutstandingTotal);
        });
        Assert.True(dash.ActiveStudents >= 2);
    }

    [Fact]
    public async Task Admin_DeactivatedStudent_IsNotCounted_AndPaymentsLowerTheOutstandingTotal()
    {
        var client = await factory.ClientForAsync(SisApiFactory.AdminId);
        await factory.AddStudentAsync(3902);
        var before = await (await client.GetAsync("/api/dashboard/admin")).ReadAsync<AdminDashboardDto>();

        var userId = await factory.UserIdAsync(3902);
        await client.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        await factory.WithDbAsync(async db =>
        {
            var charge = await db.Set<Charge>().Include(c => c.Payments).FirstAsync(c => c.Status != ChargeStatus.Paid);
            charge.ApplyPayment(1m, userId, DateTime.UtcNow, null);
            await db.SaveChangesAsync();
        });

        var after = await (await client.GetAsync("/api/dashboard/admin")).ReadAsync<AdminDashboardDto>();
        Assert.Equal(before.ActiveStudents - 1, after.ActiveStudents);
        Assert.Equal(before.OutstandingTotal - 1m, after.OutstandingTotal);
    }
}
