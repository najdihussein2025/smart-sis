using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Grading;

/// <summary>T38: a student's grades this term and transcript (PB-09). Only the signed-in student's own data.</summary>
public sealed class StudentGradesApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Grading = "/api/instructor/grading/sections";

    private async Task SaveAsync(int sectionId, int componentId, params (int EnrollmentId, decimal Score)[] scores)
    {
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var res = await instructor.PutAsJsonAsync($"{Grading}/{sectionId}/components/{componentId}",
            new SaveScoresRequest([.. scores.Select(s => new ScoreEntryRequest(s.EnrollmentId, s.Score))]));
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    private async Task FinalizeAsync(int sectionId)
    {
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var res = await instructor.PostAsync($"{Grading}/{sectionId}/finalize", null);
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
    }

    /// <summary>Drops the given enrollments so the course's components are unlocked again for the next test.</summary>
    private Task UnlockAsync(int[] enrollmentIds) => factory.WithDbAsync(async db =>
    {
        foreach (var e in await db.Set<Enrollment>().Where(x => enrollmentIds.Contains(x.Id)).ToListAsync()) e.Status = EnrollmentStatus.Dropped;
        await db.SaveChangesAsync();
    });

    private async Task<T> GetAsync<T>(long universityId, string url)
    {
        var client = await factory.ClientForAsync(universityId);
        var res = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.ReadAsync<T>();
    }

    [Fact]
    public async Task My_InProgress_ShowsComponentsNotGradedAndRunningPercent()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Midterm", 50, 40), ("Final", 100, 60));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [52001, 52002]);
        await SaveAsync(sectionId, components[0], (enrollments[0], 45), (enrollments[1], 10));

        var my = await GetAsync<MyGradesDto>(52001, "/api/grades/my");

        Assert.NotNull(my.TermId);
        var course = Assert.Single(my.Courses);
        Assert.Equal(("CSC 310", EnrollmentStatus.Enrolled, 3), (course.CourseCode, course.Status, course.Credits));
        // 45/50 = 90 % of the 40 % midterm → 36 earned, 90 % running; final not graded.
        Assert.Equal((40m, 36m, 90m), (course.GradedWeight, course.Earned, course.RunningPct!.Value));
        Assert.Null(course.LetterGrade);
        Assert.Equal(["Midterm", "Final"], course.Components.Select(c => c.Name));
        Assert.Equal((45m, 90m), (course.Components[0].Score!.Value, course.Components[0].Pct!.Value));
        Assert.Equal((40m, 50m), (course.Components[0].Weight, course.Components[0].MaxGrade));
        Assert.Null(course.Components[1].Score);
        Assert.Null(course.Components[1].Pct);
        Assert.Null(my.TermGpa);
        Assert.Null(my.CumulativeGpa);
        Assert.Equal(0, my.CreditsEarned);
    }

    [Fact]
    [Trait("Rule", "G4")]
    public async Task My_AfterFinalize_ShowsLetterAndGpa()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Final", 100, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [52011]);
        await SaveAsync(sectionId, components[0], (enrollments[0], 88.5m)); // 88.5 → 89 = A-
        await FinalizeAsync(sectionId);

        var my = await GetAsync<MyGradesDto>(52011, "/api/grades/my");

        var course = Assert.Single(my.Courses);
        Assert.Equal((EnrollmentStatus.Completed, 89m, "A-", 3.7m), (course.Status, course.FinalPct!.Value, course.LetterGrade!, course.GradePoints!.Value));
        Assert.Equal((3.7m, 3.7m, 3), (my.TermGpa!.Value, my.CumulativeGpa!.Value, my.CreditsEarned));
        await UnlockAsync(enrollments);
    }

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task My_OnlyTheSignedInStudentsOwnScores()
    {
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Final", 100, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [52021, 52022]);
        await SaveAsync(sectionId, components[0], (enrollments[0], 70), (enrollments[1], 95));

        var mine = await GetAsync<MyGradesDto>(52022, "/api/grades/my");

        Assert.Equal(95m, Assert.Single(Assert.Single(mine.Courses).Components).Score);
    }

    [Fact]
    public async Task My_SeededStudent_SeesSeededComponentsWithSomeNotGraded()
    {
        var my = await GetAsync<MyGradesDto>(SisApiFactory.StudentId, "/api/grades/my");

        Assert.Equal(["CSC 210", "MAT 201"], my.Courses.Select(c => c.CourseCode));
        Assert.All(my.Courses, c => Assert.Equal(100, c.Components.Sum(x => x.Weight)));
        Assert.Contains(my.Courses.SelectMany(c => c.Components), c => c.Score is not null);
        Assert.Contains(my.Courses.SelectMany(c => c.Components), c => c.Score is null);
        // Demo Student passed CSC 101 last term with a B.
        Assert.Equal((3.0m, 3), (my.CumulativeGpa!.Value, my.CreditsEarned));
    }

    [Fact]
    [Trait("Rule", "G6")]
    public async Task Transcript_SeededStudent_PastTermFromTermResults_CurrentTermInProgress()
    {
        var transcript = await GetAsync<TranscriptDto>(SisApiFactory.StudentId, "/api/grades/transcript");

        Assert.Equal((3.0m, 3, 3), (transcript.CumulativeGpa!.Value, transcript.CreditsAttempted, transcript.CreditsEarned));
        Assert.Equal(2, transcript.Terms.Count);
        var past = transcript.Terms[0];
        Assert.StartsWith("Spring", past.TermName);
        Assert.False(past.IsCurrent);
        Assert.Equal((3.0m, 3.0m, 3, 3), (past.TermGpa!.Value, past.CumulativeGpa!.Value, past.CreditsAttempted, past.CreditsEarned));
        var csc101 = Assert.Single(past.Courses);
        Assert.Equal(("CSC 101", "B", 3.0m, true), (csc101.CourseCode, csc101.LetterGrade!, csc101.GradePoints!.Value, csc101.CountsInGpa));

        var current = transcript.Terms[1];
        Assert.True(current.IsCurrent);
        Assert.Null(current.TermGpa);
        Assert.All(current.Courses, c => Assert.Equal((EnrollmentStatus.Enrolled, null), (c.Status, c.LetterGrade)));
    }

    [Fact]
    [Trait("Rule", "G5")]
    public async Task Transcript_Retake_OldAttemptMarked_CumulativeUsesLatest()
    {
        await factory.AddStudentAsync(52031);
        await factory.AddCompletedAsync(52031, "CSC 310", 0m, "F");
        var components = await factory.SetCurrentComponentsAsync("CSC 310", ("Final", 100, 100));
        var (sectionId, enrollments) = await factory.NewSectionAsync("CSC 310", [52031]);
        await SaveAsync(sectionId, components[0], (enrollments[0], 80)); // B 3.0
        await FinalizeAsync(sectionId);

        var transcript = await GetAsync<TranscriptDto>(52031, "/api/grades/transcript");

        Assert.Equal(2, transcript.Terms.Count);
        Assert.False(Assert.Single(transcript.Terms[0].Courses).CountsInGpa);
        Assert.Equal((0m, 0m, 3, 0), (transcript.Terms[0].TermGpa!.Value, transcript.Terms[0].CumulativeGpa!.Value, transcript.Terms[0].CreditsAttempted, transcript.Terms[0].CreditsEarned));
        Assert.Equal((3.0m, 3.0m), (transcript.Terms[1].TermGpa!.Value, transcript.Terms[1].CumulativeGpa!.Value));
        Assert.Equal((3.0m, 3, 3), (transcript.CumulativeGpa!.Value, transcript.CreditsAttempted, transcript.CreditsEarned));
        await UnlockAsync(enrollments);
    }

    [Fact]
    [Trait("Rule", "A3")]
    public async Task AbsenceDrop_ShowsFAndCountsInGpa()
    {
        await factory.SetCurrentComponentsAsync("CSC 310", ("Final", 100, 100));
        await factory.AddStudentAsync(52041);
        await factory.AddCompletedAsync(52041, "CSC 101", 4.0m, "A");
        var (_, enrollments) = await factory.NewSectionAsync("CSC 310", [52041]);
        await factory.WithDbAsync(async db =>
        {
            (await db.Set<Enrollment>().SingleAsync(e => e.Id == enrollments[0])).DropForAbsence(DateTime.UtcNow);
            await db.SaveChangesAsync();
        });

        var my = await GetAsync<MyGradesDto>(52041, "/api/grades/my");
        var course = Assert.Single(my.Courses);
        Assert.Equal((EnrollmentStatus.DroppedAbsence, "F", 0m), (course.Status, course.LetterGrade!, course.GradePoints!.Value));
        Assert.Equal((0m, 2.0m), (my.TermGpa!.Value, my.CumulativeGpa!.Value));

        // No TermResults row for this term yet: the transcript computes it the same way.
        var transcript = await GetAsync<TranscriptDto>(52041, "/api/grades/transcript");
        Assert.Equal((0m, 2.0m, 3, 0), (transcript.Terms[1].TermGpa!.Value, transcript.Terms[1].CumulativeGpa!.Value, transcript.Terms[1].CreditsAttempted, transcript.Terms[1].CreditsEarned));
    }

    [Fact]
    public async Task NewStudent_NothingYet_EmptyViews()
    {
        await factory.AddStudentAsync(52051);

        var my = await GetAsync<MyGradesDto>(52051, "/api/grades/my");
        Assert.Empty(my.Courses);
        Assert.Null(my.CumulativeGpa);

        var transcript = await GetAsync<TranscriptDto>(52051, "/api/grades/transcript");
        Assert.Empty(transcript.Terms);
        Assert.Null(transcript.CumulativeGpa);
    }

    [Fact]
    public async Task NoCurrentTerm_NoTermButCumulativeStays()
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        await factory.WithDbAsync(async db =>
        {
            (await db.Set<Term>().SingleAsync(t => t.IsCurrent)).IsCurrent = false;
            await db.SaveChangesAsync();
        });
        try
        {
            var my = await (await client.GetAsync("/api/grades/my")).ReadAsync<MyGradesDto>();
            Assert.Null(my.TermId);
            Assert.Empty(my.Courses);
            Assert.Equal(3.0m, my.CumulativeGpa);
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
