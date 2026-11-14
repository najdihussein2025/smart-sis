using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Interfaces;
using SmartSis.Api.Features.Dashboard.Interfaces;
using SmartSis.Api.Features.Dashboard.Models.Dtos;
using SmartSis.Api.Features.Dashboard.Rules;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Interfaces;
using SmartSis.Api.Features.Finance.Interfaces;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Schedule.Interfaces;

namespace SmartSis.Api.Features.Dashboard.Services;

/// <summary>
/// Puts the dashboard numbers together from the owning features' read services, so GPA, credit cap, balance and
/// attendance standing are computed by the same rules as their own pages. Every service reads the user from the token.
/// "Today" is the UTC date, the same day attendance uses.
/// </summary>
public sealed class DashboardService(
    IStudentGradesService grades,
    IEnrollmentService enrollments,
    IScheduleService schedule,
    IMyFinanceService finance,
    INotificationInboxService notifications,
    IAttendanceService attendance,
    IGradeEntryService gradeEntry,
    IDashboardRepository repository,
    TimeProvider clock) : IDashboardService
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public async Task<StudentDashboardDto> GetStudentAsync(CancellationToken ct)
    {
        var today = (int)Today.DayOfWeek;
        var myGrades = await grades.GetMineAsync(ct);
        var mine = await enrollments.GetMineAsync(ct);
        var week = await schedule.GetStudentScheduleAsync(ct);
        var money = await finance.GetMineAsync(ct);
        var unread = await notifications.UnreadCountAsync(ct);
        var absences = await attendance.GetMineAsync(ct);
        var current = absences.Courses.Where(c => c.EnrollmentStatus == EnrollmentStatus.Enrolled).ToList();

        return new StudentDashboardDto(
            mine.TermName ?? myGrades.TermName,
            myGrades.CumulativeGpa,
            myGrades.TermGpa,
            myGrades.CreditsEarned,
            mine.EnrolledCredits,
            mine.MaxCredits,
            week.Items.Count(i => i.DayOfWeek == today),
            money.OutstandingBalance,
            money.OverdueBalance,
            money.NextDueDate,
            unread.Count,
            current.Count(c => c.Standing != AbsenceStanding.Perfect),
            current.Count(c => c.Standing == AbsenceStanding.Critical));
    }

    public async Task<InstructorDashboardDto> GetInstructorAsync(CancellationToken ct)
    {
        var teaching = await attendance.GetTeachingSectionsAsync(ct);
        var today = teaching.Today;
        var meetings = teaching.Sections
            .SelectMany(s => s.Meetings.Select(m => new WeeklyMeeting(m.Id, m.DayOfWeek, s.TermStart, s.TermEnd)))
            .ToList();
        var taken = await repository.TakenSessionsAsync(
            [.. meetings.Select(m => m.MeetingId)], AttendanceDue.WeekStart(today), today, ct);

        var gradingSections = (await gradeEntry.ListSectionsAsync(ct)).Where(s => !s.IsFinalized).ToList();

        return new InstructorDashboardDto(
            teaching.TermName,
            today,
            teaching.Sections.Count,
            meetings.Count(m => m.DayOfWeek == (int)today.DayOfWeek),
            gradingSections.Sum(s => s.NotGraded),
            gradingSections.Count(s => s.NotGraded > 0),
            AttendanceDue.Count(meetings, taken.ToHashSet(), today));
    }

    public Task<AdminDashboardDto> GetAdminAsync(CancellationToken ct) => repository.GetAdminAsync(ct);
}
