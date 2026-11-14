using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Interfaces;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Attendance.Rules;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Attendance.Services;

/// <summary>
/// PB-10: an instructor takes attendance for a meeting of a section they teach on a date. The session, its records and
/// everything the absences cause are saved in one transaction.
/// </summary>
public sealed class AttendanceService(
    IAttendanceRepository attendance,
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

                await uow.SaveChangesAsync(ct);
                var absent = request.Records.Count(r => r.Status == AttendanceStatus.Absent);
                return new SubmitAttendanceResultDto(session.Id, request.Records.Count - absent, absent, []);
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
        m.Credits * 3, rows);
}
