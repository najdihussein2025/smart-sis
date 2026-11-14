using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Interfaces;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Attendance.Rules;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Attendance.Repositories;

public sealed class AttendanceRepository(SisDbContext db) : IAttendanceRepository
{
    public async Task<(int Id, string Name)?> GetCurrentTermAsync(CancellationToken ct)
    {
        var t = await db.Set<Term>().AsNoTracking().Where(x => x.IsCurrent).Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync(ct);
        return t is null ? null : (t.Id, t.Name);
    }

    public async Task<List<TeachingSectionDto>> GetTeachingSectionsAsync(int instructorId, int termId, CancellationToken ct)
    {
        var rows = await db.Set<Section>().AsNoTracking()
            .Where(s => s.InstructorId == instructorId && s.TermId == termId)
            .OrderBy(s => s.Course.Code).ThenBy(s => s.SectionCode)
            .Select(s => new
            {
                s.Id, s.Course.Code, s.Course.Name, s.SectionCode, s.Course.Credits, s.Term.StartDate, s.Term.EndDate,
                Enrolled = db.Set<Enrollment>().Count(e => e.SectionId == s.Id && e.Status == EnrollmentStatus.Enrolled),
                Meetings = s.Meetings.OrderBy(m => m.DayOfWeek).ThenBy(m => m.StartTime)
                    .Select(m => new AttendanceMeetingDto(m.Id, m.DayOfWeek, m.StartTime, m.EndTime, m.Room.Name, m.Type)).ToList(),
            })
            .ToListAsync(ct);
        return rows.Select(r => new TeachingSectionDto(
            r.Id, r.Code, r.Name, r.SectionCode, r.Credits, AbsencePolicy.LimitFor(r.Credits), r.StartDate, r.EndDate, r.Enrolled, r.Meetings)).ToList();
    }

    public Task<MeetingInfo?> GetMeetingAsync(int meetingId, CancellationToken ct) =>
        db.Set<SectionMeeting>().AsNoTracking().Where(m => m.Id == meetingId)
            .Select(m => new MeetingInfo(
                m.Id, m.SectionId, m.Section.InstructorId, m.DayOfWeek, m.StartTime, m.Type,
                m.Section.Course.Code, m.Section.SectionCode, m.Section.Course.Credits, m.Section.Term.StartDate, m.Section.Term.EndDate))
            .FirstOrDefaultAsync(ct);

    public async Task<(bool Exists, int? InstructorId)> GetSectionInstructorAsync(int sectionId, CancellationToken ct)
    {
        var s = await db.Set<Section>().AsNoTracking().Where(x => x.Id == sectionId).Select(x => new { x.InstructorId }).FirstOrDefaultAsync(ct);
        return s is null ? (false, null) : (true, s.InstructorId);
    }

    public Task<AttendanceSession?> FindSessionAsync(int meetingId, DateOnly date, CancellationToken ct) =>
        db.Set<AttendanceSession>().AsNoTracking().FirstOrDefaultAsync(s => s.MeetingId == meetingId && s.SessionDate == date, ct);

    public Task<AttendanceSession?> FindSessionAsync(int sessionId, CancellationToken ct) =>
        db.Set<AttendanceSession>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public Task<List<RosterStudent>> GetRosterAsync(int sectionId, CancellationToken ct) =>
        db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.SectionId == sectionId && e.Status == EnrollmentStatus.Enrolled)
            .OrderBy(e => e.Student.User.FullName).ThenBy(e => e.Student.User.UniversityId)
            .Select(e => new RosterStudent(e.Id, e.StudentId, e.Student.User.UniversityId, e.Student.User.FullName, e.Student.User.Email))
            .ToListAsync(ct);

    public Task<List<RecordedMark>> GetMarksAsync(int sessionId, CancellationToken ct) =>
        db.Set<AttendanceRecord>().AsNoTracking()
            .Where(r => r.SessionId == sessionId)
            .OrderBy(r => r.Enrollment.Student.User.FullName).ThenBy(r => r.Enrollment.Student.User.UniversityId)
            .Select(r => new RecordedMark(r.EnrollmentId, r.Enrollment.Student.User.UniversityId, r.Enrollment.Student.User.FullName, r.Status))
            .ToListAsync(ct);

    public async Task<Dictionary<int, int>> GetAbsenceCountsAsync(IReadOnlyCollection<int> enrollmentIds, CancellationToken ct) =>
        await db.Set<AttendanceRecord>().AsNoTracking()
            .Where(r => enrollmentIds.Contains(r.EnrollmentId) && r.Status == AttendanceStatus.Absent)
            .GroupBy(r => r.EnrollmentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public async Task<Dictionary<int, List<AbsenceAlertLevel>>> GetSentAlertsAsync(IReadOnlyCollection<int> enrollmentIds, CancellationToken ct)
    {
        var rows = await db.Set<AbsenceAlert>().AsNoTracking()
            .Where(a => enrollmentIds.Contains(a.EnrollmentId))
            .Select(a => new { a.EnrollmentId, a.Level })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.EnrollmentId).ToDictionary(g => g.Key, g => g.Select(r => r.Level).ToList());
    }

    public Task<List<SessionSummaryDto>> ListSessionsAsync(int sectionId, CancellationToken ct) =>
        db.Set<AttendanceSession>().AsNoTracking()
            .Where(s => s.SectionId == sectionId)
            .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.Meeting.StartTime)
            .Select(s => new SessionSummaryDto(
                s.Id, s.MeetingId, s.SessionDate, s.Meeting.DayOfWeek, s.Meeting.StartTime, s.Meeting.Type,
                s.Records.Count(r => r.Status == AttendanceStatus.Present),
                s.Records.Count(r => r.Status == AttendanceStatus.Absent),
                s.SubmittedAt))
            .ToListAsync(ct);

    public void Add(AttendanceSession session) => db.Add(session);

    public void Add(AbsenceAlert alert) => db.Add(alert);
}
