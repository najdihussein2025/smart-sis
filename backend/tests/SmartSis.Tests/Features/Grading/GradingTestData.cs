using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Grading;

/// <summary>Grading helpers over the seeded simulated data (SR-23).</summary>
internal static class GradingTestData
{
    public static async Task<int> CourseIdAsync(this SisApiFactory f, string code)
    {
        var id = 0;
        await f.WithDbAsync(async db => id = await db.Set<Course>().Where(c => c.Code == code).Select(c => c.Id).SingleAsync());
        return id;
    }

    public static async Task<int> CurrentTermIdAsync(this SisApiFactory f)
    {
        var id = 0;
        await f.WithDbAsync(async db => id = await db.Set<Term>().Where(t => t.IsCurrent).Select(t => t.Id).SingleAsync());
        return id;
    }

    /// <summary>A new empty (non-current) term, so a test's components don't meet another test's.</summary>
    public static async Task<int> NewTermAsync(this SisApiFactory f)
    {
        var term = new Term
        {
            Name = $"Test {Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2030, 2, 1), EndDate = new DateOnly(2030, 6, 1),
            EnrollStart = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), EnrollEnd = new DateTime(2030, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            AddDropStart = new DateTime(2030, 1, 10, 0, 0, 0, DateTimeKind.Utc), AddDropEnd = new DateTime(2030, 2, 10, 0, 0, 0, DateTimeKind.Utc),
        };
        await f.WithDbAsync(async db =>
        {
            db.Add(term);
            await db.SaveChangesAsync();
        });
        return term.Id;
    }

    /// <summary>Replaces the components of a course in the current term (removing their scores too).</summary>
    public static async Task<int[]> SetCurrentComponentsAsync(this SisApiFactory f, string courseCode, params (string Name, decimal Max, decimal Weight)[] items)
    {
        var courseId = await f.CourseIdAsync(courseCode);
        var termId = await f.CurrentTermIdAsync();
        var created = new List<GradeComponent>();
        await f.WithDbAsync(async db =>
        {
            var old = await db.Set<GradeComponent>().Where(c => c.CourseId == courseId && c.TermId == termId).ToListAsync();
            var oldIds = old.Select(c => c.Id).ToList();
            db.RemoveRange(await db.Set<StudentGrade>().Where(g => oldIds.Contains(g.ComponentId)).ToListAsync());
            db.RemoveRange(old);
            foreach (var (name, max, weight) in items)
            {
                var c = new GradeComponent { CourseId = courseId, TermId = termId, Name = name, MaxGrade = max, Weight = weight };
                created.Add(c);
                db.Add(c);
            }
            await db.SaveChangesAsync();
        });
        return [.. created.Select(c => c.Id)];
    }
}
