using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// Grading demo data (SIMULATED, SR-23): components for every current-term course, some scores already entered (finals not yet),
/// and TermResults for the finished past term. Scores are deterministic so the demo looks the same on every machine.
/// </summary>
public sealed class GradingDevSeed : IDevSeedStep
{
    public int Order => 100;

    /// <summary>Components per course code: name, max grade, weight (each course adds up to 100).</summary>
    private static readonly Dictionary<string, (string Name, decimal Max, decimal Weight)[]> Plans = new()
    {
        ["CSC 101"] = [("Quiz", 20, 10), ("Midterm", 50, 30), ("Project", 100, 20), ("Final", 100, 40)],
        ["CSC 210"] = [("Lab work", 100, 20), ("Midterm", 100, 30), ("Final", 100, 50)],
        ["CSC 310"] = [("Midterm", 50, 30), ("Project", 100, 30), ("Final", 100, 40)],
        ["MAT 201"] = [("Assignments", 40, 20), ("Midterm", 100, 30), ("Final", 100, 50)],
    };

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        // EF InMemory (integration tests) never runs migrations, so copy the GradeScales reference rows from the model's HasData.
        // SQL Server already has them from the migration.
        if (!db.Database.IsRelational() && !await db.Set<GradeScale>().AnyAsync(ct))
        {
            foreach (var row in db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(GradeScale))!.GetSeedData())
                db.Add(new GradeScale
                {
                    Id = (int)row[nameof(GradeScale.Id)]!, Letter = (string)row[nameof(GradeScale.Letter)]!,
                    StatusLabel = (string)row[nameof(GradeScale.StatusLabel)]!, GpaPoints = (decimal)row[nameof(GradeScale.GpaPoints)]!,
                    MinPct = (int)row[nameof(GradeScale.MinPct)]!, MaxPct = (int)row[nameof(GradeScale.MaxPct)]!,
                });
            await db.SaveChangesAsync(ct);
        }

        var term = await db.Set<Term>().FirstOrDefaultAsync(t => t.IsCurrent, ct);
        var instructorId = await db.Set<User>().Where(u => u.Role == UserRole.Instructor).OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync(ct);
        if (term is null || instructorId is null) return;
        if (await db.Set<GradeComponent>().AnyAsync(ct)) return;

        var courses = await db.Set<Section>().Where(s => s.TermId == term.Id)
            .Select(s => new { s.CourseId, s.Course.Code }).Distinct().ToListAsync(ct);
        var components = new Dictionary<int, List<GradeComponent>>();
        foreach (var course in courses)
        {
            if (!Plans.TryGetValue(course.Code, out var plan)) continue;
            components[course.CourseId] = [.. plan.Select(p => new GradeComponent
            {
                CourseId = course.CourseId, TermId = term.Id, Name = p.Name, MaxGrade = p.Max, Weight = p.Weight,
            })];
            db.AddRange(components[course.CourseId]);
        }
        await db.SaveChangesAsync(ct);

        // Scores: the first component for everyone, the second for every other student, nothing after that (finals pending).
        var enrolled = await db.Set<Enrollment>()
            .Where(e => e.Status == EnrollmentStatus.Enrolled && e.Section.TermId == term.Id)
            .OrderBy(e => e.Id).Select(e => new { e.Id, e.Section.CourseId }).ToListAsync(ct);
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        for (var i = 0; i < enrolled.Count; i++)
        {
            if (!components.TryGetValue(enrolled[i].CourseId, out var list)) continue;
            var graded = i % 2 == 0 ? 2 : 1;
            foreach (var (component, n) in list.Take(graded).Select((c, n) => (c, n)))
            {
                // 62 % … 97 % of the max, varying by student and component.
                var pct = 62 + (enrolled[i].Id * 7 + n * 11) % 36;
                db.Add(new StudentGrade
                {
                    EnrollmentId = enrolled[i].Id, ComponentId = component.Id, GradedBy = instructorId.Value, GradedAt = now.AddDays(-3),
                    Score = Math.Round(component.MaxGrade * pct / 100m, 1, MidpointRounding.AwayFromZero),
                });
            }
        }
        await db.SaveChangesAsync(ct);

        // TermResults for students with finished courses (the past term), built by the same code as finalizing (rule G6).
        var termResults = services.GetRequiredService<ITermResultService>();
        var students = await db.Set<Enrollment>()
            .Where(e => e.Status == EnrollmentStatus.Completed || e.Status == EnrollmentStatus.DroppedAbsence)
            .Select(e => e.StudentId).Distinct().ToListAsync(ct);
        foreach (var studentId in students) await termResults.RebuildAsync(studentId, ct);
    }
}
