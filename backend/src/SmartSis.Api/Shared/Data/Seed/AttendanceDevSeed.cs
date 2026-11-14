using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// Simulated attendance (SR-23) for the current term's CSC 101 A, CSC 210 A and MAT 201 A: every meeting from the
/// term's start until a week ago is taken (the last week is left for the instructor to try). Demo Student (3001)
/// missed 3 MAT 201 classes (orange "Warning") and none of CSC 210 ("Perfect"). Nobody reaches an alert threshold.
/// </summary>
public sealed class AttendanceDevSeed : IDevSeedStep
{
    public int Order => 200;

    /// <summary>(student university id, course code) → which of the course's sessions (0-based, oldest first) they missed.</summary>
    private static readonly Dictionary<(long Student, string Course), int[]> Missed = new()
    {
        [(3001, "MAT 201")] = [1, 3, 4],
        [(3002, "CSC 210")] = [2],
        [(3005, "CSC 101")] = [0, 5],
    };

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        if (await db.Set<AttendanceSession>().AnyAsync(ct)) return;

        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var lastDay = DateOnly.FromDateTime(now).AddDays(-8);

        var sections = await db.Set<Section>()
            .Where(s => s.Term.IsCurrent && s.InstructorId != null && new[] { "CSC 101", "CSC 210", "MAT 201" }.Contains(s.Course.Code) && s.SectionCode == "A")
            .Select(s => new { s.Id, s.Course.Code, InstructorId = s.InstructorId!.Value, s.Term.StartDate, Meetings = s.Meetings.ToList() })
            .ToListAsync(ct);

        foreach (var section in sections)
        {
            var enrolled = await db.Set<Enrollment>()
                .Where(e => e.SectionId == section.Id && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => new { e.Id, e.Student.User.UniversityId })
                .ToListAsync(ct);
            if (enrolled.Count == 0) continue;

            var index = 0;
            foreach (var (meeting, date) in MeetingDates(section.Meetings, section.StartDate, lastDay))
            {
                var session = new AttendanceSession
                {
                    SectionId = section.Id, MeetingId = meeting.Id, SessionDate = date, SubmittedBy = section.InstructorId,
                    SubmittedAt = date.ToDateTime(meeting.EndTime, DateTimeKind.Utc),
                };
                foreach (var e in enrolled)
                {
                    var absent = Missed.TryGetValue((e.UniversityId, section.Code), out var missed) && missed.Contains(index);
                    session.Records.Add(new AttendanceRecord { EnrollmentId = e.Id, Status = absent ? AttendanceStatus.Absent : AttendanceStatus.Present });
                }
                db.Add(session);
                index++;
            }
        }
    }

    /// <summary>Every (meeting, date) from <paramref name="from"/> to <paramref name="to"/>, oldest first.</summary>
    private static IEnumerable<(SectionMeeting Meeting, DateOnly Date)> MeetingDates(List<SectionMeeting> meetings, DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            foreach (var m in meetings.Where(m => m.DayOfWeek == (int)d.DayOfWeek).OrderBy(m => m.StartTime))
                yield return (m, d);
    }
}
