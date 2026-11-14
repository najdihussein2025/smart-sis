using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Interfaces;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Attendance.Rules;
using SmartSis.Api.Features.Enrollments.Interfaces;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Email;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Attendance.Services;

/// <summary>
/// PB-10: an instructor takes attendance for a meeting of a section they teach on a date. The session, its records and
/// everything the absences cause (AbsenceAlerts, queued emails, notifications, auto-drops) are saved in one transaction.
/// </summary>
public sealed class AttendanceService(
    IAttendanceRepository attendance,
    IEnrollmentService enrollments,
    IEmailQueue email,
    INotificationService notifications,
    ICurrentUser me,
    IUnitOfWork uow,
    TimeProvider clock) : IAttendanceService
{
    public const string AlreadyTaken = "attendance_already_taken";
    public const string NotOnRoster = "not_on_roster";
    public const string MissingStudents = "students_missing";

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task<TeachingSectionsDto> GetTeachingSectionsAsync(CancellationToken ct)
    {
        var term = await attendance.GetCurrentTermAsync(ct);
        if (term is null) return new TeachingSectionsDto(null, null, Today, []);
        var sections = await attendance.GetTeachingSectionsAsync(me.UserId, term.Value.Id, ct);
        return new TeachingSectionsDto(term.Value.Id, term.Value.Name, Today, sections);
    }

    public async Task<AttendanceSheetDto> GetSheetAsync(int meetingId, DateOnly date, CancellationToken ct)
    {
        var meeting = await OwnMeetingAsync(meetingId, ct);
        var session = await attendance.FindSessionAsync(meetingId, date, ct);
        if (session is not null) return await TakenSheetAsync(meeting, session, ct);

        SessionDatePolicy.Check(meeting.DayOfWeek, date, meeting.TermStart, meeting.TermEnd, Today);
        var roster = await attendance.GetRosterAsync(meeting.SectionId, ct);
        var absences = await attendance.GetAbsenceCountsAsync(roster.Select(r => r.EnrollmentId).ToList(), ct);
        var rows = roster.Select(r => new AttendanceSheetRowDto(r.EnrollmentId, r.UniversityId, r.FullName, null, absences.GetValueOrDefault(r.EnrollmentId))).ToList();
        return Sheet(meeting, date, null, null, rows);
    }

    public async Task<AttendanceSheetDto> GetSessionAsync(int sessionId, CancellationToken ct)
    {
        var session = await attendance.FindSessionAsync(sessionId, ct) ?? throw NotFoundException.For<AttendanceSession>(sessionId);
        var meeting = await OwnMeetingAsync(session.MeetingId, ct);
        return await TakenSheetAsync(meeting, session, ct);
    }

    public async Task<IReadOnlyList<SessionSummaryDto>> ListSessionsAsync(int sectionId, CancellationToken ct)
    {
        var (exists, instructorId) = await attendance.GetSectionInstructorAsync(sectionId, ct);
        if (!exists) throw NotFoundException.For<Section>(sectionId);
        if (instructorId != me.UserId) throw new ForbiddenException("You can only see attendance for sections you teach.");
        return await attendance.ListSessionsAsync(sectionId, ct);
    }

    public async Task<SubmitAttendanceResultDto> SubmitAsync(SubmitAttendanceRequest request, CancellationToken ct)
    {
        var instructorId = me.UserId;
        try
        {
            return await uow.ExecuteInTransactionAsync(async () =>
            {
                var meeting = await OwnMeetingAsync(request.MeetingId, ct);
                SessionDatePolicy.Check(meeting.DayOfWeek, request.SessionDate, meeting.TermStart, meeting.TermEnd, Today);

                // Rule A4 / BG_06: checked here for a clear message; the unique index catches a race (handled below).
                if (await attendance.FindSessionAsync(meeting.MeetingId, request.SessionDate, ct) is not null)
                    throw AlreadyTakenError(meeting, request.SessionDate);

                var roster = await attendance.GetRosterAsync(meeting.SectionId, ct);
                CheckMarksMatchRoster(roster, request.Records);

                var session = new AttendanceSession
                {
                    SectionId = meeting.SectionId, MeetingId = meeting.MeetingId, SessionDate = request.SessionDate,
                    SubmittedBy = instructorId, SubmittedAt = Now,
                };
                foreach (var mark in request.Records)
                    session.Records.Add(new AttendanceRecord { EnrollmentId = mark.EnrollmentId, Status = mark.Status });
                attendance.Add(session);

                var absentIds = request.Records.Where(r => r.Status == AttendanceStatus.Absent).Select(r => r.EnrollmentId).ToList();
                var alerts = await ApplyAbsenceRulesAsync(meeting, roster.Where(r => absentIds.Contains(r.EnrollmentId)).ToList(), instructorId, ct);

                await uow.SaveChangesAsync(ct);
                return new SubmitAttendanceResultDto(session.Id, request.Records.Count - absentIds.Count, absentIds.Count, alerts);
            }, ct);
        }
        catch (DbUpdateException)
        {
            // Two submissions raced and the unique index (MeetingId, SessionDate) refused the second one.
            if (await attendance.FindSessionAsync(request.MeetingId, request.SessionDate, ct) is null) throw;
            var meeting = await OwnMeetingAsync(request.MeetingId, ct);
            throw AlreadyTakenError(meeting, request.SessionDate);
        }
    }

    public async Task<MyAttendanceDto> GetMineAsync(CancellationToken ct)
    {
        var term = await attendance.GetCurrentTermAsync(ct);
        if (term is null) return new MyAttendanceDto(null, null, []);

        var courses = await attendance.GetMyCoursesAsync(me.UserId, term.Value.Id, ct);
        var marks = (await attendance.GetMarksOfAsync(courses.Select(c => c.EnrollmentId).ToList(), ct)).ToLookup(m => m.EnrollmentId);
        var rows = courses.Select(c =>
        {
            var mine = marks[c.EnrollmentId].ToList();
            var absent = mine.Where(m => m.Status == AttendanceStatus.Absent)
                .Select(m => new AbsenceDateDto(m.Date, m.DayOfWeek, m.StartTime, m.Type)).ToList();
            var limit = AbsencePolicy.LimitFor(c.Credits);
            return new CourseAttendanceDto(
                c.SectionId, c.CourseCode, c.CourseName, c.SectionCode, c.Credits, absent.Count, limit, Math.Max(0, limit - absent.Count),
                AbsencePolicy.StandingFor(absent.Count, limit), c.Status, mine.Count, absent);
        }).ToList();
        return new MyAttendanceDto(term.Value.Id, term.Value.Name, rows);
    }

    /// <summary>
    /// Rules A2 and A3 for the students marked absent: each one's absences now include this session (one more than saved).
    /// A due alert is recorded in AbsenceAlerts (so it is sent once) with an email and a notification; at limit + 1 the
    /// student is dropped through the Enrollments feature. Nothing is saved here: the submission's transaction commits it.
    /// </summary>
    private async Task<List<AbsenceAlertDto>> ApplyAbsenceRulesAsync(
        MeetingInfo meeting, List<RosterStudent> absent, int instructorId, CancellationToken ct)
    {
        if (absent.Count == 0) return [];
        var ids = absent.Select(a => a.EnrollmentId).ToList();
        var saved = await attendance.GetAbsenceCountsAsync(ids, ct);
        var sent = await attendance.GetSentAlertsAsync(ids, ct);
        var limit = AbsencePolicy.LimitFor(meeting.Credits);
        var alerts = new List<AbsenceAlertDto>();

        foreach (var student in absent)
        {
            var absences = saved.GetValueOrDefault(student.EnrollmentId) + 1;
            var decision = AbsencePolicy.Evaluate(absences, limit, sent.GetValueOrDefault(student.EnrollmentId) ?? []);

            if (decision.AutoDrop)
                await enrollments.DropForAbsenceAsync(student.EnrollmentId, instructorId, ct);

            if (decision.Alert is { } level)
            {
                attendance.Add(new AbsenceAlert { EnrollmentId = student.EnrollmentId, Level = level, AbsenceCount = absences, SentAt = Now });
                Tell(student, meeting.CourseCode, level, absences, limit);
                alerts.Add(new AbsenceAlertDto(student.EnrollmentId, student.FullName, level, absences, limit));
            }
        }
        return alerts;
    }

    /// <summary>Queues the email (rule C2) and the in-app notification for an alert.</summary>
    private void Tell(RosterStudent student, string course, AbsenceAlertLevel level, int absences, int limit)
    {
        const string link = "/student/attendance";
        var (subject, body) = level == AbsenceAlertLevel.AutoDropped
            ? EmailTemplates.AbsenceAutoDropped(student.FullName, course, absences, limit)
            : EmailTemplates.AbsenceWarning(student.FullName, course, absences, limit);
        email.Enqueue(student.Email, subject, body);

        switch (level)
        {
            case AbsenceAlertLevel.Warning:
                notifications.Notify(student.StudentId, NotificationType.AbsenceWarning, $"Attendance warning: {course}",
                    $"You have {absences} absences in {course}. The limit is {limit}; going over it drops you from the course with an F.", link);
                break;
            case AbsenceAlertLevel.Limit:
                notifications.Notify(student.StudentId, NotificationType.AbsenceLimit, $"Absence limit reached: {course}",
                    $"You have reached the absence limit ({limit}) in {course}. One more absence drops you from the course with an F.", link);
                break;
            default:
                notifications.Notify(student.StudentId, NotificationType.AutoDropped, $"Dropped from {course}",
                    $"You were dropped from {course} after {absences} absences (limit {limit}). The course is recorded as F in your GPA.", link);
                break;
        }
    }

    /// <summary>The meeting, after checking the section is taught by the signed-in instructor.</summary>
    private async Task<MeetingInfo> OwnMeetingAsync(int meetingId, CancellationToken ct)
    {
        var meeting = await attendance.GetMeetingAsync(meetingId, ct) ?? throw NotFoundException.For<SectionMeeting>(meetingId);
        if (meeting.InstructorId != me.UserId)
            throw new ForbiddenException("You can only take attendance for sections you teach.");
        return meeting;
    }

    /// <summary>Every enrolled student is marked exactly once, and nobody else.</summary>
    private static void CheckMarksMatchRoster(List<RosterStudent> roster, IReadOnlyList<AttendanceMarkRequest> marks)
    {
        var enrolled = roster.Select(r => r.EnrollmentId).ToHashSet();
        if (marks.Any(m => !enrolled.Contains(m.EnrollmentId)))
            throw new BusinessRuleException("A marked student isn't enrolled in this section. Reload the list and try again.", NotOnRoster);

        var missing = enrolled.Except(marks.Select(m => m.EnrollmentId)).Count();
        if (missing > 0)
            throw new BusinessRuleException(
                missing == 1 ? "1 student isn't marked. Mark every student Present or Absent." : $"{missing} students aren't marked. Mark every student Present or Absent.",
                MissingStudents);
    }

    private static BusinessRuleException AlreadyTakenError(MeetingInfo m, DateOnly date) => new(
        $"Attendance for {m.CourseCode} {m.SectionCode} on {SessionDatePolicy.Format(date)} at {m.StartTime:HH\\:mm} was already submitted.",
        AlreadyTaken);

    private async Task<AttendanceSheetDto> TakenSheetAsync(MeetingInfo meeting, AttendanceSession session, CancellationToken ct)
    {
        var marks = await attendance.GetMarksAsync(session.Id, ct);
        var absences = await attendance.GetAbsenceCountsAsync(marks.Select(m => m.EnrollmentId).ToList(), ct);
        var rows = marks.Select(m => new AttendanceSheetRowDto(m.EnrollmentId, m.UniversityId, m.FullName, m.Status, absences.GetValueOrDefault(m.EnrollmentId))).ToList();
        return Sheet(meeting, session.SessionDate, session.Id, session.SubmittedAt, rows);
    }

    private static AttendanceSheetDto Sheet(MeetingInfo m, DateOnly date, int? sessionId, DateTime? submittedAt, List<AttendanceSheetRowDto> rows) => new(
        m.SectionId, m.CourseCode, m.SectionCode, m.MeetingId, m.DayOfWeek, m.StartTime, m.Type, date, sessionId, submittedAt,
        AbsencePolicy.LimitFor(m.Credits), rows);
}
