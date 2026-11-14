using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Cart.Models.Entities;
using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Courses.Interfaces;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Courses.Repositories;

/// <summary>Catalog reads. Seats are counted from Enrollments (Enrolled or Completed), never stored (rule E8).</summary>
public sealed class CatalogRepository(SisDbContext db) : ICatalogRepository
{
    public Task<CatalogTerm?> GetCurrentTermAsync(CancellationToken ct) =>
        db.Set<Term>().AsNoTracking().Where(t => t.IsCurrent)
            .Select(t => new CatalogTerm(t.Id, t.Name, t.EnrollStart, t.EnrollEnd, t.AddDropStart, t.AddDropEnd))
            .FirstOrDefaultAsync(ct);

    public Task<int?> GetStudentMajorIdAsync(int studentId, CancellationToken ct) =>
        db.Set<Student>().Where(s => s.UserId == studentId).Select(s => (int?)s.MajorId).FirstOrDefaultAsync(ct);

    public async Task<List<CatalogCourseDto>> ListCoursesAsync(
        int termId, int? majorId, CourseType? type, string? search, int studentId, CancellationToken ct)
    {
        var sections = db.Set<Section>().Where(s => s.TermId == termId);
        var q = db.Set<Course>().AsNoTracking().Where(c => sections.Any(s => s.CourseId == c.Id));
        if (majorId is not null)
            q = q.Where(c => c.MajorCourses.Any(mc => mc.MajorId == majorId && (type == null || mc.Type == type)));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            q = q.Where(c => c.Code.ToLower().Contains(term) || c.Name.ToLower().Contains(term));
        }

        var rows = await q.OrderBy(c => c.Code).Select(c => new
        {
            c.Id, c.Code, c.Name, c.Credits, DepartmentName = c.Department.Name,
            Type = c.MajorCourses.Where(mc => mc.MajorId == majorId).Select(mc => (CourseType?)mc.Type).FirstOrDefault(),
            SectionCount = sections.Count(s => s.CourseId == c.Id),
            Capacity = sections.Where(s => s.CourseId == c.Id).Sum(s => s.Capacity),
        }).ToListAsync(ct);

        var courseIds = rows.Select(r => r.Id).ToList();
        var taken = await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.Section.TermId == termId && courseIds.Contains(e.Section.CourseId)
                        && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed))
            .GroupBy(e => e.Section.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, ct);
        var (enrolled, inCart) = await StudentCourseStatusAsync(termId, studentId, ct);

        return rows.Select(r => new CatalogCourseDto(
            r.Id, r.Code, r.Name, r.Credits, r.DepartmentName, r.Type, r.SectionCount,
            Math.Max(0, r.Capacity - taken.GetValueOrDefault(r.Id)),
            enrolled.Contains(r.Id) ? CatalogStatus.Enrolled : inCart.Contains(r.Id) ? CatalogStatus.InCart : CatalogStatus.Available))
            .ToList();
    }

    public async Task<CatalogCourseDetailDto?> GetCourseAsync(int courseId, int? termId, int studentId, CancellationToken ct)
    {
        var course = await db.Set<Course>().AsNoTracking().Where(c => c.Id == courseId).Select(c => new
        {
            c.Id, c.Code, c.Name, c.Credits, c.Description, DepartmentName = c.Department.Name,
            Prerequisites = c.Prerequisites.OrderBy(p => p.Prerequisite.Code)
                .Select(p => new { p.PrerequisiteId, p.Prerequisite.Code, p.Prerequisite.Name }).ToList(),
        }).FirstOrDefaultAsync(ct);
        if (course is null) return null;

        var prereqIds = course.Prerequisites.Select(p => p.PrerequisiteId).ToList();
        var passed = await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Completed && e.GradePoints >= 1.0m
                        && prereqIds.Contains(e.Section.CourseId))
            .Select(e => e.Section.CourseId).Distinct().ToListAsync(ct);

        var sections = new List<CatalogSectionDto>();
        if (termId is not null)
        {
            var enrollments = db.Set<Enrollment>();
            var rows = await db.Set<Section>().AsNoTracking()
                .Where(s => s.CourseId == courseId && s.TermId == termId)
                .OrderBy(s => s.SectionCode)
                .Select(s => new
                {
                    s.Id, s.SectionCode, InstructorName = s.Instructor != null ? s.Instructor.User.FullName : null, s.Capacity,
                    Taken = enrollments.Count(e => e.SectionId == s.Id
                                                   && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed)),
                    Meetings = s.Meetings.OrderBy(m => m.DayOfWeek).ThenBy(m => m.StartTime)
                        .Select(m => new SectionMeetingDto(m.Id, m.DayOfWeek, m.StartTime, m.EndTime, m.RoomId, m.Room.Name, m.Type))
                        .ToList(),
                }).ToListAsync(ct);

            var sectionIds = rows.Select(r => r.Id).ToList();
            var enrolledIn = await db.Set<Enrollment>().AsNoTracking()
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled && sectionIds.Contains(e.SectionId))
                .Select(e => e.SectionId).ToListAsync(ct);
            var inCart = await db.Set<CartItem>().AsNoTracking()
                .Where(c => c.StudentId == studentId && sectionIds.Contains(c.SectionId))
                .Select(c => c.SectionId).ToListAsync(ct);

            sections = rows.Select(r => new CatalogSectionDto(
                r.Id, r.SectionCode, r.InstructorName, r.Capacity, r.Taken, Math.Max(0, r.Capacity - r.Taken), r.Meetings,
                enrolledIn.Contains(r.Id) ? CatalogStatus.Enrolled : inCart.Contains(r.Id) ? CatalogStatus.InCart : CatalogStatus.Available))
                .ToList();
        }

        return new CatalogCourseDetailDto(
            course.Id, course.Code, course.Name, course.Credits, course.Description, course.DepartmentName,
            course.Prerequisites.Select(p => new CatalogPrerequisiteDto(p.PrerequisiteId, p.Code, p.Name, passed.Contains(p.PrerequisiteId))).ToList(),
            sections);
    }

    public Task<List<CatalogMajorDto>> ListMajorsAsync(CancellationToken ct) =>
        db.Set<Major>().AsNoTracking().OrderBy(m => m.Name).Select(m => new CatalogMajorDto(m.Id, m.Name)).ToListAsync(ct);

    /// <summary>Course ids the student is enrolled in this term, and course ids with a section in their cart.</summary>
    private async Task<(HashSet<int> Enrolled, HashSet<int> InCart)> StudentCourseStatusAsync(int termId, int studentId, CancellationToken ct)
    {
        var enrolled = await db.Set<Enrollment>().AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled && e.Section.TermId == termId)
            .Select(e => e.Section.CourseId).ToListAsync(ct);
        var inCart = await db.Set<CartItem>().AsNoTracking()
            .Where(c => c.StudentId == studentId && c.Section.TermId == termId)
            .Select(c => c.Section.CourseId).ToListAsync(ct);
        return ([.. enrolled], [.. inCart]);
    }
}
