using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Cart.Models.Entities;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Tests.Infrastructure;

/// <summary>Helpers over the seeded simulated data (SR-23). Seeded sections all belong to the current term.</summary>
public static class TestData
{
    /// <summary>JSON options matching the API (camelCase, enums as strings).</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage res) =>
        (await res.Content.ReadFromJsonAsync<T>(Json))!;

    public static async Task<int> UserIdAsync(this SisApiFactory f, long universityId)
    {
        var id = 0;
        await f.WithDbAsync(async db => id = await db.Set<User>().Where(u => u.UniversityId == universityId).Select(u => u.Id).SingleAsync());
        return id;
    }

    /// <summary>Section id of a seeded section, e.g. ("CSC 210", "B").</summary>
    public static async Task<int> SectionIdAsync(this SisApiFactory f, string courseCode, string sectionCode = "A")
    {
        var id = 0;
        await f.WithDbAsync(async db => id = await db.Set<Section>()
            .Where(s => s.Course.Code == courseCode && s.SectionCode == sectionCode && s.Term.IsCurrent)
            .Select(s => s.Id).SingleAsync());
        return id;
    }

    /// <summary>Empties the student's cart and enrollments, so each test starts clean.</summary>
    public static Task ResetStudentAsync(this SisApiFactory f, long universityId = SisApiFactory.StudentId) =>
        f.WithDbAsync(async db =>
        {
            var userId = await db.Set<User>().Where(u => u.UniversityId == universityId).Select(u => u.Id).SingleAsync();
            db.RemoveRange(await db.Set<CartItem>().Where(c => c.StudentId == userId).ToListAsync());
            var enrollmentIds = await db.Set<Enrollment>().Where(e => e.StudentId == userId).Select(e => e.Id).ToListAsync();
            db.RemoveRange(await db.Set<EnrollmentAction>().Where(a => a.StudentId == userId).ToListAsync());
            await RemoveEnrollmentChildrenAsync(db, enrollmentIds);
            db.RemoveRange(await db.Set<Enrollment>().Where(e => e.StudentId == userId).ToListAsync());
            await db.SaveChangesAsync();
        });

    private static async Task RemoveEnrollmentChildrenAsync(SisDbContext db, List<int> enrollmentIds)
    {
        db.RemoveRange(await db.Set<StudentGrade>().Where(g => enrollmentIds.Contains(g.EnrollmentId)).ToListAsync());
        db.RemoveRange(await db.Set<AttendanceRecord>().Where(r => enrollmentIds.Contains(r.EnrollmentId)).ToListAsync());
        db.RemoveRange(await db.Set<AbsenceAlert>().Where(a => enrollmentIds.Contains(a.EnrollmentId)).ToListAsync());
    }

    /// <summary>Opens or closes the current term's windows relative to now.</summary>
    public static Task SetWindowAsync(this SisApiFactory f, TermWindowState state) =>
        f.WithDbAsync(async db =>
        {
            var t = await db.Set<Term>().SingleAsync(x => x.IsCurrent);
            var now = DateTime.UtcNow;
            (t.EnrollStart, t.EnrollEnd, t.AddDropStart, t.AddDropEnd) = state switch
            {
                TermWindowState.Enrollment => (now.AddDays(-14), now.AddDays(7), now.AddDays(7), now.AddDays(21)),
                TermWindowState.AddDrop => (now.AddDays(-21), now.AddDays(-7), now.AddDays(-7), now.AddDays(7)),
                _ => (now.AddDays(-40), now.AddDays(-30), now.AddDays(-30), now.AddDays(-20)),
            };
            await db.SaveChangesAsync();
        });

    /// <summary>Puts a completed attempt of a course on the student's record (in a past term).</summary>
    public static Task AddCompletedAsync(this SisApiFactory f, long universityId, string courseCode, decimal gradePoints, string letter = "C") =>
        f.WithDbAsync(async db =>
        {
            var userId = await db.Set<User>().Where(u => u.UniversityId == universityId).Select(u => u.Id).SingleAsync();
            var past = await db.Set<Term>().FirstOrDefaultAsync(t => t.Name == "Spring 2000");
            if (past is null)
            {
                past = new Term
                {
                    Name = "Spring 2000", StartDate = new DateOnly(2000, 2, 1), EndDate = new DateOnly(2000, 6, 1),
                    EnrollStart = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), EnrollEnd = new DateTime(2000, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                    AddDropStart = new DateTime(2000, 1, 10, 0, 0, 0, DateTimeKind.Utc), AddDropEnd = new DateTime(2000, 2, 10, 0, 0, 0, DateTimeKind.Utc),
                };
                db.Add(past);
            }
            var course = await db.Set<SmartSis.Api.Features.Courses.Models.Entities.Course>().SingleAsync(c => c.Code == courseCode);
            var section = await db.Set<Section>().FirstOrDefaultAsync(s => s.CourseId == course.Id && s.Term.Name == "Spring 2000");
            if (section is null)
            {
                section = new Section { Course = course, Term = past, SectionCode = "P", Capacity = 100 };
                db.Add(section);
            }
            db.Add(new Enrollment
            {
                StudentId = userId, Section = section, Status = EnrollmentStatus.Completed, GradePoints = gradePoints, LetterGrade = letter,
                FinalPct = 70, EnrolledAt = new DateTime(2000, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            });
            await db.SaveChangesAsync();
        });

    /// <summary>Creates another simulated student of the seeded major. Returns the university id.</summary>
    public static async Task<long> AddStudentAsync(this SisApiFactory f, long universityId, int? maxCredits = null)
    {
        await f.WithDbAsync(async db =>
        {
            if (await db.Set<User>().AnyAsync(u => u.UniversityId == universityId)) return;
            var seeded = await db.Set<Student>().Include(s => s.User).FirstAsync();
            var user = new User
            {
                UniversityId = universityId, Email = $"student{universityId}@smartsis.local", FullName = $"Test Student {universityId}",
                Role = UserRole.Student, PasswordHash = seeded.User.PasswordHash,
            };
            db.Add(user);
            db.Add(new Student { User = user, MajorId = seeded.MajorId, CampusId = seeded.CampusId, EnrollmentDate = new DateOnly(2025, 9, 1), MaxCreditsOverride = maxCredits });
            await db.SaveChangesAsync();
        });
        return universityId;
    }
}

public enum TermWindowState
{
    Closed,
    Enrollment,
    AddDrop,
}
