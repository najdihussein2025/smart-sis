using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Schedule.Interfaces;
using SmartSis.Api.Features.Schedule.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Schedule.Repositories;

public sealed class ScheduleRepository(SisDbContext db) : IScheduleRepository
{
    public async Task<(int Id, string Name)?> GetCurrentTermAsync(CancellationToken ct)
    {
        var t = await db.Set<Term>().AsNoTracking().Where(x => x.IsCurrent).Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync(ct);
        return t is null ? null : (t.Id, t.Name);
    }

    public Task<List<ScheduleItemDto>> GetStudentItemsAsync(int studentId, int termId, CancellationToken ct)
    {
        var sectionIds = db.Set<Enrollment>()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled)
            .Select(e => e.SectionId);
        return Project(db.Set<SectionMeeting>().Where(m => m.Section.TermId == termId && sectionIds.Contains(m.SectionId)), ct);
    }

    public Task<List<ScheduleItemDto>> GetInstructorItemsAsync(int instructorId, int termId, CancellationToken ct) =>
        Project(db.Set<SectionMeeting>().Where(m => m.Section.TermId == termId && m.Section.InstructorId == instructorId), ct);

    private static Task<List<ScheduleItemDto>> Project(IQueryable<SectionMeeting> meetings, CancellationToken ct) =>
        meetings.AsNoTracking()
            .OrderBy(m => m.DayOfWeek).ThenBy(m => m.StartTime).ThenBy(m => m.Section.Course.Code)
            .Select(m => new ScheduleItemDto(
                m.SectionId, m.Id, m.Section.Course.Code, m.Section.Course.Name, m.Section.SectionCode,
                m.DayOfWeek, m.StartTime, m.EndTime, m.Room.Name,
                m.Section.Instructor != null ? m.Section.Instructor.User.FullName : null,
                m.Type))
            .ToListAsync(ct);
}
