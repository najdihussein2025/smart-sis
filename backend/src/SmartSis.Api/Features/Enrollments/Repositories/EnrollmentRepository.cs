using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Interfaces;
using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Enrollments.Rules;
using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Enrollments.Repositories;

public sealed class EnrollmentRepository(SisDbContext db) : IEnrollmentRepository
{
    public Task<CurrentTerm?> GetCurrentTermAsync(CancellationToken ct) =>
        db.Set<Term>().AsNoTracking().Where(t => t.IsCurrent)
            .Select(t => new CurrentTerm(t.Id, t.Name, t.EnrollStart, t.EnrollEnd, t.AddDropStart, t.AddDropEnd))
            .FirstOrDefaultAsync(ct);

    public Task<int> GetMaxCreditsAsync(int studentId, CancellationToken ct) =>
        db.Set<Student>().Where(s => s.UserId == studentId).Select(s => s.MaxCreditsOverride ?? s.Major.DefaultMaxCredits).FirstOrDefaultAsync(ct);

    public async Task<List<HeldSection>> GetHeldSectionsAsync(int studentId, int termId, CancellationToken ct)
    {
        var rows = await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled && e.Section.TermId == termId)
            .Select(e => new
            {
                e.SectionId, e.Section.CourseId, e.Section.Course.Code, e.Section.SectionCode, e.Section.Course.Credits,
                Meetings = e.Section.Meetings.Select(m => new MeetingTime(m.DayOfWeek, m.StartTime, m.EndTime)).ToList(),
            }).ToListAsync(ct);
        return rows.Select(r => new HeldSection(r.SectionId, r.CourseId, r.Code, $"{r.Code} {r.SectionCode}", r.Credits, r.Meetings)).ToList();
    }

    public async Task<HashSet<int>> GetPassedCourseIdsAsync(int studentId, CancellationToken ct) =>
    [
        .. await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Completed && e.GradePoints >= 1.0m)
            .Select(e => e.Section.CourseId).Distinct().ToListAsync(ct),
    ];

    public async Task<LockedSection?> LockSectionAsync(int sectionId, CancellationToken ct)
    {
        // Rule E6: the lock is held until the transaction ends, so two students can't take the last seat together.
        if (db.Database.IsRelational())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Sections WITH (UPDLOCK, ROWLOCK) WHERE Id = {sectionId}", ct);

        var s = await db.Set<Section>().AsNoTracking().Where(x => x.Id == sectionId).Select(x => new
        {
            x.Id, x.TermId, x.CourseId, x.Course.Code, x.SectionCode, x.Course.Credits, x.Capacity,
            Meetings = x.Meetings.Select(m => new MeetingTime(m.DayOfWeek, m.StartTime, m.EndTime)).ToList(),
            Prerequisites = x.Course.Prerequisites.Select(p => new Prerequisite(p.PrerequisiteId, p.Prerequisite.Code)).ToList(),
        }).FirstOrDefaultAsync(ct);
        if (s is null) return null;

        var taken = await db.Set<Enrollment>().CountAsync(
            e => e.SectionId == sectionId && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed), ct);
        return new LockedSection(s.Id, s.TermId,
            new SectionCandidate(s.Id, s.CourseId, s.Code, $"{s.Code} {s.SectionCode}", s.Credits, s.Meetings, s.Prerequisites, s.Capacity, taken));
    }

    public Task<List<EnrolledSectionDto>> ListMineAsync(int studentId, int termId, CancellationToken ct) =>
        db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled && e.Section.TermId == termId)
            .OrderBy(e => e.Section.Course.Code)
            .Select(e => new EnrolledSectionDto(
                e.Id, e.SectionId, e.Section.CourseId, e.Section.Course.Code, e.Section.Course.Name, e.Section.Course.Credits,
                e.Section.SectionCode,
                e.Section.Instructor != null ? e.Section.Instructor.User.FullName : null,
                e.Section.Meetings.OrderBy(m => m.DayOfWeek).ThenBy(m => m.StartTime)
                    .Select(m => new SectionMeetingDto(m.Id, m.DayOfWeek, m.StartTime, m.EndTime, m.RoomId, m.Room.Name, m.Type))
                    .ToList(),
                e.EnrolledAt))
            .ToListAsync(ct);

    public Task<Enrollment?> FindActiveAsync(int studentId, int sectionId, CancellationToken ct) =>
        db.Set<Enrollment>().FirstOrDefaultAsync(e => e.StudentId == studentId && e.SectionId == sectionId && e.Status == EnrollmentStatus.Enrolled, ct);

    public Task<Enrollment?> FindAsync(int enrollmentId, CancellationToken ct) =>
        db.Set<Enrollment>().FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

    public void Add(Enrollment enrollment) => db.Set<Enrollment>().Add(enrollment);
    public void Add(EnrollmentAction action) => db.Set<EnrollmentAction>().Add(action);
}
