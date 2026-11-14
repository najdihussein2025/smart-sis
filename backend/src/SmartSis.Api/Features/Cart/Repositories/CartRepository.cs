using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Cart.Interfaces;
using SmartSis.Api.Features.Cart.Models.Dtos;
using SmartSis.Api.Features.Cart.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Cart.Repositories;

public sealed class CartRepository(SisDbContext db) : ICartRepository
{
    public async Task<(int Id, string Name, DateTime EnrollStart, DateTime EnrollEnd, DateTime AddDropStart, DateTime AddDropEnd)?> GetCurrentTermAsync(CancellationToken ct)
    {
        var t = await db.Set<Term>().AsNoTracking().FirstOrDefaultAsync(x => x.IsCurrent, ct);
        return t is null ? null : (t.Id, t.Name, t.EnrollStart, t.EnrollEnd, t.AddDropStart, t.AddDropEnd);
    }

    public async Task<List<CartItemDto>> ListAsync(int studentId, int termId, CancellationToken ct)
    {
        var enrollments = db.Set<Enrollment>();
        var rows = await db.Set<CartItem>().AsNoTracking()
            .Where(c => c.StudentId == studentId && c.Section.TermId == termId)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .Select(c => new
            {
                c.SectionId, c.Section.CourseId, c.Section.Course.Code, c.Section.Course.Name, c.Section.Course.Credits,
                c.Section.SectionCode,
                InstructorName = c.Section.Instructor != null ? c.Section.Instructor.User.FullName : null,
                c.Section.Capacity,
                Taken = enrollments.Count(e => e.SectionId == c.SectionId
                                               && (e.Status == EnrollmentStatus.Enrolled || e.Status == EnrollmentStatus.Completed)),
                Meetings = c.Section.Meetings.OrderBy(m => m.DayOfWeek).ThenBy(m => m.StartTime)
                    .Select(m => new SectionMeetingDto(m.Id, m.DayOfWeek, m.StartTime, m.EndTime, m.RoomId, m.Room.Name, m.Type))
                    .ToList(),
                c.CreatedAt,
            }).ToListAsync(ct);

        return rows.Select(r => new CartItemDto(
            r.SectionId, r.CourseId, r.Code, r.Name, r.Credits, r.SectionCode, r.InstructorName, r.Capacity,
            Math.Max(0, r.Capacity - r.Taken), r.Meetings, r.CreatedAt)).ToList();
    }

    public async Task<int> GetEnrolledCreditsAsync(int studentId, int termId, CancellationToken ct) =>
        await db.Set<Enrollment>()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled && e.Section.TermId == termId)
            .SumAsync(e => (int?)e.Section.Course.Credits, ct) ?? 0;

    public async Task<int> GetMaxCreditsAsync(int studentId, CancellationToken ct) =>
        await db.Set<Student>().Where(s => s.UserId == studentId)
            .Select(s => s.MaxCreditsOverride ?? s.Major.DefaultMaxCredits)
            .FirstOrDefaultAsync(ct);

    public Task<CartSectionInfo?> GetSectionAsync(int sectionId, CancellationToken ct) =>
        db.Set<Section>().AsNoTracking().Where(s => s.Id == sectionId)
            .Select(s => new CartSectionInfo(s.Id, s.TermId, s.Term.IsCurrent, s.Course.Code + " " + s.SectionCode))
            .FirstOrDefaultAsync(ct);

    public Task<bool> ContainsAsync(int studentId, int sectionId, CancellationToken ct) =>
        db.Set<CartItem>().AnyAsync(c => c.StudentId == studentId && c.SectionId == sectionId, ct);

    public Task<bool> IsEnrolledInSectionAsync(int studentId, int sectionId, CancellationToken ct) =>
        db.Set<Enrollment>().AnyAsync(e => e.StudentId == studentId && e.SectionId == sectionId && e.Status == EnrollmentStatus.Enrolled, ct);

    public Task<CartItem?> FindAsync(int studentId, int sectionId, CancellationToken ct) =>
        db.Set<CartItem>().FirstOrDefaultAsync(c => c.StudentId == studentId && c.SectionId == sectionId, ct);

    public void Add(CartItem item) => db.Set<CartItem>().Add(item);
    public void Remove(CartItem item) => db.Set<CartItem>().Remove(item);
}
