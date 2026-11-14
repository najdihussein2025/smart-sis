using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Grading;

/// <summary>
/// T35: instructor grade entry (rule G2), finalizing (G3, G5) and the TermResults rebuild (G6). Each test builds its own section with
/// its own simulated students, and resets the components of the course it uses.
/// </summary>
public sealed class GradeEntryApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/instructor/grading/sections";

    private Task<HttpClient> InstructorAsync() => factory.ClientForAsync(SisApiFactory.InstructorId);

    private static Task<HttpResponseMessage> SaveAsync(HttpClient client, int sectionId, int componentId, params ScoreEntryRequest[] scores) =>
        client.PutAsJsonAsync($"{Url}/{sectionId}/components/{componentId}", new SaveScoresRequest(scores));

    private static ScoreEntryRequest S(int enrollmentId, decimal? score) => new(enrollmentId, score);

    private static async Task<string?> CodeAsync(HttpResponseMessage res) =>
        (await res.ReadAsync<ProblemDetails>()).Extensions.TryGetValue("code", out var c) ? c?.ToString() : null;

    [Fact]
    [Trait("Rule", "G2")]
    public async Task Roster_ExistingScoresArePrefilled_AndNotGradedIsCounted()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Midterm", 50, 40), ("Final", 100, 60));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51001, 51002, 51003]);
        var client = await InstructorAsync();

        var saved = await SaveAsync(client, sectionId, components[0], S(enrollments[0], 42.5m), S(enrollments[2], 0));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var roster = await (await client.GetAsync($"{Url}/{sectionId}/components/{components[0]}")).ReadAsync<ComponentRosterDto>();
        Assert.Equal(["Test Student 51001", "Test Student 51002", "Test Student 51003"], roster.Students.Select(s => s.FullName));
        Assert.Equal([42.5m, null, 0m], roster.Students.Select(s => s.Score));
        Assert.Equal((2, 1), (roster.Component.Graded, roster.Component.NotGraded));
        Assert.False(roster.IsFinalized);

        var section = await (await client.GetAsync($"{Url}/{sectionId}")).ReadAsync<GradingSectionDto>();
        Assert.Equal(3, section.StudentCount);
        Assert.Equal(100, section.TotalWeight);
        Assert.Equal(1 + 3, section.NotGraded);
        Assert.Equal(["Midterm", "Final"], section.Components.Select(c => c.Name));

        var list = await (await client.GetAsync(Url)).ReadAsync<List<TeachingSectionDto>>();
        var mine = list.Single(s => s.SectionId == sectionId);
        Assert.Equal((3, 2, 4), (mine.StudentCount, mine.ComponentCount, mine.NotGraded));
    }

    [Fact]
    [Trait("Rule", "G2")]
    public async Task SaveScores_UpdatesAndClears_OneGradePerEnrollmentAndComponent()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Final", 100, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51011, 51012]);
        var client = await InstructorAsync();

        await SaveAsync(client, sectionId, components[0], S(enrollments[0], 70), S(enrollments[1], 80));
        var res = await SaveAsync(client, sectionId, components[0], S(enrollments[0], 75), S(enrollments[1], null));
        var roster = await res.ReadAsync<ComponentRosterDto>();

        Assert.Equal([75m, null], roster.Students.Select(s => s.Score));
        var instructorUserId = await factory.UserIdAsync(SisApiFactory.InstructorId);
        await factory.WithDbAsync(async db =>
        {
            var grades = await db.Set<StudentGrade>().Where(g => enrollments.Contains(g.EnrollmentId)).ToListAsync();
            var grade = Assert.Single(grades);
            Assert.Equal((75m, instructorUserId), (grade.Score, grade.GradedBy));
        });
    }

    [Fact]
    public async Task SaveScores_AboveMax_Returns400OnTheField()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Quiz", 10, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51021]);

        var res = await SaveAsync(await InstructorAsync(), sectionId, components[0], S(enrollments[0], 10.5m));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.ReadAsync<ValidationProblemDetails>();
        Assert.Equal("Enter a score between 0 and 10.", Assert.Single(problem.Errors["scores[0].score"]));
    }

    [Fact]
    public async Task SaveScores_NegativeOrDuplicate_Returns400()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Quiz", 10, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51031]);
        var client = await InstructorAsync();

        var negative = await SaveAsync(client, sectionId, components[0], S(enrollments[0], -1));
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
        Assert.Contains("scores[0].score", (await negative.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        var twice = await SaveAsync(client, sectionId, components[0], S(enrollments[0], 1), S(enrollments[0], 2));
        Assert.Equal(HttpStatusCode.BadRequest, twice.StatusCode);
    }

    [Fact]
    public async Task SaveScores_StudentOfAnotherSection_Returns400()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Quiz", 10, 100));
        var (sectionId, _) = await factory.NewSectionAsync("CSC 310", [51041]);
        var (_, others) = await factory.NewSectionAsync("CSC 310", [51042]);

        var res = await SaveAsync(await InstructorAsync(), sectionId, components[0], S(others[0], 5));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("scores[0].enrollmentId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task SectionNotTaught_Returns403_ForEveryAction()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Quiz", 10, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51051], taughtBySeededInstructor: false);
        var client = await InstructorAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Url}/{sectionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Url}/{sectionId}/components/{components[0]}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SaveAsync(client, sectionId, components[0], S(enrollments[0], 5))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"{Url}/{sectionId}/finalize", null)).StatusCode);
        Assert.DoesNotContain((await (await client.GetAsync(Url)).ReadAsync<List<TeachingSectionDto>>()), s => s.SectionId == sectionId);
    }

    [Fact]
    public async Task UnknownSectionOrForeignComponent_Returns404()
    {
        var foreign = await factory.SetCurrentComponentsAsync("MAT 201", ("Quiz", 10, 100));
        await factory.SetCurrentComponentsAsync("CSC 310", ("Quiz", 10, 100));
        var (sectionId, _) = await factory.NewSectionAsync("CSC 310", [51061]);
        var client = await InstructorAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/{sectionId}/components/{foreign[0]}")).StatusCode);
    }

    [Fact]
    public async Task Finalize_MissingScore_Returns409()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Midterm", 50, 40), ("Final", 100, 60));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51071, 51072]);
        var client = await InstructorAsync();
        await SaveAsync(client, sectionId, components[0], S(enrollments[0], 40), S(enrollments[1], 40));
        await SaveAsync(client, sectionId, components[1], S(enrollments[0], 90));

        var res = await client.PostAsync($"{Url}/{sectionId}/finalize", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.ReadAsync<ProblemDetails>();
        Assert.Equal("scores_missing", problem.Extensions["code"]!.ToString());
        Assert.StartsWith("1 score is missing (Test Student 51072: Final)", problem.Title);
        await factory.WithDbAsync(async db =>
            Assert.All(await db.Set<Enrollment>().Where(e => enrollments.Contains(e.Id)).ToListAsync(),
                e => Assert.Equal(EnrollmentStatus.Enrolled, e.Status)));
    }

    [Fact]
    public async Task Finalize_WeightsNot100_Returns409()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Midterm", 50, 40));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51081]);
        var client = await InstructorAsync();
        await SaveAsync(client, sectionId, components[0], S(enrollments[0], 40));

        var res = await client.PostAsync($"{Url}/{sectionId}/finalize", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("weights_incomplete", await CodeAsync(res));
    }

    [Fact]
    [Trait("Rule", "G3")]
    [Trait("Rule", "G6")]
    public async Task Finalize_CompletesStudents_WithRoundedLetters_AndBuildsTermResults()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Midterm", 50, 40), ("Final", 100, 60));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51091, 51092]);
        var client = await InstructorAsync();
        // 51091: 25/50 × 40 = 20 + 66/100 × 60 = 39.6 → 59.6 → 60 = D. 51092: full marks → 100 = A+.
        await SaveAsync(client, sectionId, components[0], S(enrollments[0], 25), S(enrollments[1], 50));
        await SaveAsync(client, sectionId, components[1], S(enrollments[0], 66), S(enrollments[1], 100));

        var res = await client.PostAsync($"{Url}/{sectionId}/finalize", null);

        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        var result = await res.ReadAsync<FinalizeResultDto>();
        Assert.Equal([(60m, "D", 1.0m), (100m, "A+", 4.0m)], result.Students.Select(s => (s.FinalPct, s.LetterGrade, s.GradePoints)));

        var termId = await factory.CurrentTermIdAsync();
        var studentId = await factory.UserIdAsync(51091);
        await factory.WithDbAsync(async db =>
        {
            var e = await db.Set<Enrollment>().SingleAsync(x => x.Id == enrollments[0]);
            Assert.Equal((EnrollmentStatus.Completed, 60m, "D", 1.0m, true), (e.Status, e.FinalPct!.Value, e.LetterGrade!, e.GradePoints!.Value, e.CountsInGpa));
            var tr = await db.Set<TermResult>().SingleAsync(r => r.StudentId == studentId && r.TermId == termId);
            Assert.Equal((1.0m, 1.0m, 3, 3), (tr.TermGpa, tr.CumulativeGpa, tr.CreditsAttempted, tr.CreditsEarned));
        });

        // Finalized: the roster says so, scores are locked, a second finalize is refused, and the admin can't change components.
        Assert.True((await (await client.GetAsync($"{Url}/{sectionId}/components/{components[0]}")).ReadAsync<ComponentRosterDto>()).IsFinalized);
        var resave = await SaveAsync(client, sectionId, components[0], S(enrollments[0], 50));
        Assert.Equal(HttpStatusCode.Conflict, resave.StatusCode);
        Assert.Equal("grades_finalized", await CodeAsync(resave));
        var again = await client.PostAsync($"{Url}/{sectionId}/finalize", null);
        Assert.Equal("already_finalized", await CodeAsync(again));
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        var edit = await admin.PutAsJsonAsync($"/api/admin/grade-components/{components[0]}", new UpdateGradeComponentRequest("Midterm", 50, 40));
        Assert.Equal("grades_finalized", await CodeAsync(edit));

        // Put the components back to an unlocked state for the other tests of this class.
        await factory.WithDbAsync(async db =>
        {
            foreach (var e in await db.Set<Enrollment>().Where(x => enrollments.Contains(x.Id)).ToListAsync()) e.Status = EnrollmentStatus.Dropped;
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    [Trait("Rule", "G5")]
    public async Task Finalize_Retake_OlderAttemptStopsCounting_AndCumulativeUsesLatest()
    {
        await factory.AddStudentAsync(51101);
        await factory.AddCompletedAsync(51101, "MAT 201", 0m, "F"); // failed in Spring 2000
        await factory.AddCompletedAsync(51101, "CSC 101", 4.0m, "A");
        var components = await factory.SetCurrentComponentsAsync("MAT 201", ("Final", 100, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("MAT 201", [51101]);
        var client = await InstructorAsync();
        await SaveAsync(client, sectionId, components[0], S(enrollments[0], 85)); // B+ 3.3

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"{Url}/{sectionId}/finalize", null)).StatusCode);

        var studentId = await factory.UserIdAsync(51101);
        await factory.WithDbAsync(async db =>
        {
            var attempts = await db.Set<Enrollment>().Where(e => e.StudentId == studentId && e.Section.Course.Code == "MAT 201")
                .OrderBy(e => e.Id).ToListAsync();
            Assert.Equal([false, true], attempts.Select(a => a.CountsInGpa));

            var rows = await db.Set<TermResult>().Where(r => r.StudentId == studentId).Include(r => r.Term)
                .OrderBy(r => r.Term.StartDate).ToListAsync();
            Assert.Equal(2, rows.Count);
            // Spring 2000: F + A = 2.0. This term: B+ 3.3; cumulative drops the old F → (3.3 + 4.0) / 2 = 3.65.
            Assert.Equal((2.0m, 2.0m, 6, 3), (rows[0].TermGpa, rows[0].CumulativeGpa, rows[0].CreditsAttempted, rows[0].CreditsEarned));
            Assert.Equal((3.3m, 3.65m, 3, 3), (rows[1].TermGpa, rows[1].CumulativeGpa, rows[1].CreditsAttempted, rows[1].CreditsEarned));
            Assert.True(rows[1].Term.IsCurrent);
        });

        await factory.WithDbAsync(async db =>
        {
            foreach (var e in await db.Set<Enrollment>().Where(x => enrollments.Contains(x.Id)).ToListAsync()) e.Status = EnrollmentStatus.Dropped;
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task AdminComponent_WithScores_CantBeDeletedOrShrunkBelowAScore()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Quiz", 20, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [51111]);
        await SaveAsync(await InstructorAsync(), sectionId, components[0], S(enrollments[0], 18));
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);

        var delete = await admin.DeleteAsync($"/api/admin/grade-components/{components[0]}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Equal("component_has_scores", await CodeAsync(delete));

        var shrink = await admin.PutAsJsonAsync($"/api/admin/grade-components/{components[0]}", new UpdateGradeComponentRequest("Quiz", 15, 100));
        Assert.Equal(HttpStatusCode.Conflict, shrink.StatusCode);
        Assert.Equal("scores_above_max", await CodeAsync(shrink));
    }

    [Fact]
    public async Task NoCurrentTerm_ListIsEmpty()
    {
        var client = await InstructorAsync();
        await factory.WithDbAsync(async db =>
        {
            (await db.Set<Term>().SingleAsync(t => t.IsCurrent)).IsCurrent = false;
            await db.SaveChangesAsync();
        });
        try
        {
            Assert.Empty(await (await client.GetAsync(Url)).ReadAsync<List<TeachingSectionDto>>());
        }
        finally
        {
            await factory.WithDbAsync(async db =>
            {
                (await db.Set<Term>().Where(t => t.Name.StartsWith("Fall")).OrderByDescending(t => t.StartDate).FirstAsync()).IsCurrent = true;
                await db.SaveChangesAsync();
            });
        }
    }
}
