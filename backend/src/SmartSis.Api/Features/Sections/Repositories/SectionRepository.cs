using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Interfaces;
using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Sections.Rules;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Models;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Sections.Repositories;

public sealed class SectionRepository(SisDbContext db) : Repository<Section>(db), ISectionRepository
{
    public async Task<PagedResult<SectionDto>> SearchAsync(int? termId, int? courseId, string? search, PageQuery page, CancellationToken ct)
    {
        var q = Set.AsNoTracking();
        if (termId is not null) q = q.Where(s => s.TermId == termId);
        if (courseId is not null) q = q.Where(s => s.CourseId == courseId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower: SQL Server's collation is case-insensitive anyway, EF InMemory (tests) is not.
            var term = search.Trim().ToLower();
            q = q.Where(s => s.Course.Code.ToLower().Contains(term) || s.Course.Name.ToLower().Contains(term));
        }

        var total = await q.CountAsync(ct);
        var items = await Project(q.OrderBy(s => s.Course.Code).ThenBy(s => s.SectionCode).Skip(page.Skip).Take(page.Take))
            .ToListAsync(ct);
        return new PagedResult<SectionDto>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public Task<SectionDto?> GetDetailAsync(int id, CancellationToken ct) =>
        Project(Set.AsNoTracking().Where(s => s.Id == id)).FirstOrDefaultAsync(ct);

    public Task<Section?> GetForUpdateAsync(int id, CancellationToken ct) =>
        Set.Include(s => s.Meetings).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> CodeExistsAsync(int courseId, int termId, string code, int? excludeId, CancellationToken ct)
    {
        var upper = code.ToUpper();
        return Set.AnyAsync(s => s.CourseId == courseId && s.TermId == termId && s.SectionCode.ToUpper() == upper
                                 && (excludeId == null || s.Id != excludeId), ct);
    }

    public Task<bool> CourseExistsAsync(int courseId, CancellationToken ct) => Db.Set<Course>().AnyAsync(c => c.Id == courseId, ct);

    public Task<bool> TermExistsAsync(int termId, CancellationToken ct) => Db.Set<Term>().AnyAsync(t => t.Id == termId, ct);

    public Task<bool> ActiveInstructorExistsAsync(int userId, CancellationToken ct) =>
        Db.Set<Instructor>().AnyAsync(i => i.UserId == userId && i.User.IsActive, ct);

    public async Task<HashSet<int>> ExistingRoomIdsAsync(IEnumerable<int> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        return [.. await Db.Set<Room>().Where(r => list.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct)];
    }

    public async Task<List<ScheduledMeeting>> GetTermMeetingsAsync(int termId, int? excludeSectionId, CancellationToken ct)
    {
        var rows = await Db.Set<SectionMeeting>().AsNoTracking()
            .Where(m => m.Section.TermId == termId && (excludeSectionId == null || m.SectionId != excludeSectionId))
            .Select(m => new
            {
                m.Section.Course.Code, m.Section.SectionCode, m.Section.InstructorId,
                m.DayOfWeek, m.StartTime, m.EndTime, m.RoomId, RoomName = m.Room.Name,
            })
            .ToListAsync(ct);
        return rows
            .Select(r => new ScheduledMeeting($"{r.Code} {r.SectionCode}", r.InstructorId, r.DayOfWeek, r.StartTime, r.EndTime, r.RoomId, r.RoomName))
            .ToList();
    }

    public Task<int> CountSeatsTakenAsync(int sectionId, CancellationToken ct) =>
        Db.Set<Enrollment>().CountAsync(e => e.SectionId == sectionId
                                             && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed), ct);

    public async Task<HashSet<int>> MeetingIdsWithAttendanceAsync(IEnumerable<int> meetingIds, CancellationToken ct)
    {
        var list = meetingIds.ToList();
        if (list.Count == 0) return [];
        return [.. await Db.Set<AttendanceSession>().Where(a => list.Contains(a.MeetingId)).Select(a => a.MeetingId).Distinct().ToListAsync(ct)];
    }

    public void RemoveMeeting(SectionMeeting meeting) => Db.Set<SectionMeeting>().Remove(meeting);

    /// <summary>Seats are counted from Enrollments (never stored on the section, rule E8).</summary>
    private IQueryable<SectionDto> Project(IQueryable<Section> sections)
    {
        var enrollments = Db.Set<Enrollment>();
        return sections.Select(s => new SectionDto(
            s.Id,
            s.CourseId,
            s.Course.Code,
            s.Course.Name,
            s.TermId,
            s.Term.Name,
            s.SectionCode,
            s.InstructorId,
            s.Instructor != null ? s.Instructor.User.FullName : null,
            s.Capacity,
            enrollments.Count(e => e.SectionId == s.Id
                                   && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed)),
            s.Meetings
                .OrderBy(m => m.DayOfWeek).ThenBy(m => m.StartTime)
                .Select(m => new SectionMeetingDto(m.Id, m.DayOfWeek, m.StartTime, m.EndTime, m.RoomId, m.Room.Name, m.Type))
                .ToList()));
    }
}
