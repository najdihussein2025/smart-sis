using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Career.Rules;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Career.CareerTestSupport;

namespace SmartSis.Tests.Features.Career;

/// <summary>
/// PB-17 / T66, T68 and PB-20 / T72, T74. Demo Student (3001) passed CSC 101 (B) last term and takes CSC 210 A and
/// MAT 201 A now; CSC 101 / 210 / 310 and MAT 201 carry seeded trait tags.
/// </summary>
public sealed class CareerRecommendationsApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private static long _next = 3950;
    private static long NextId() => Interlocked.Increment(ref _next) + 20;

    [Fact]
    public async Task Recommendations_NoAssessment_409()
    {
        var student = await NewStudentAsync(factory, NextId());
        var res = await student.GetAsync("/api/career/recommendations");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("assessment_required", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Recommendations_TopThree_FallbackExplanation_SuggestsUnpassedRoadmapCourses()
    {
        // The test factory has no AI service key, so the built-in explanation is used and the request still succeeds.
        var student = await factory.ClientForAsync(SisApiFactory.StudentId);
        var (_, answers) = await AnswerAllAsync(student);
        Assert.Equal(HttpStatusCode.Created, (await SubmitAsync(student, answers)).StatusCode);

        var rec = await (await student.GetAsync("/api/career/recommendations")).ReadAsync<RecommendationsDto>();

        Assert.Equal(3, rec.Careers.Count);
        Assert.Equal(rec.Careers.OrderByDescending(c => c.MatchPct).Select(c => c.MatchPct), rec.Careers.Select(c => c.MatchPct));
        Assert.All(rec.Careers, c => Assert.InRange(c.MatchPct, 0, 100));
        Assert.All(rec.Careers, c => Assert.NotEmpty(c.TopTraits));
        Assert.True(rec.UsesGrades); // CSC 101 (B) is tagged with traits
        Assert.Equal(ExplanationSource.Fallback, rec.ExplanationSource);
        Assert.StartsWith($"Your strongest match is {rec.Careers[0].Name}", rec.Explanation);

        // Passed (CSC 101) and current (CSC 210, MAT 201) courses are never suggested.
        var codes = rec.SuggestedCourses.Select(c => c.Code).ToList();
        Assert.DoesNotContain("CSC 101", codes);
        Assert.DoesNotContain("CSC 210", codes);
        Assert.DoesNotContain("MAT 201", codes);
        Assert.All(rec.SuggestedCourses, c => Assert.NotEmpty(c.Careers));
        if (codes.Contains("CSC 310"))
            Assert.Equal("Available", rec.SuggestedCourses.Single(c => c.Code == "CSC 310").Sections.Single().Status);
    }

    [Fact]
    public async Task Recommendations_UseTheAiExplanation_AndSendNoPersonalData()
    {
        var ai = new FakeCareerAi("You think like an engineer.");
        await using var app = WithAi(factory, ai);
        var id = NextId();
        await factory.AddStudentAsync(id);
        var student = await SignInAsync(app, id);
        var (_, answers) = await AnswerAllAsync(student, optionIndex: 1);
        Assert.Equal(HttpStatusCode.Created, (await SubmitAsync(student, answers)).StatusCode);

        var rec = await (await student.GetAsync("/api/career/recommendations")).ReadAsync<RecommendationsDto>();

        Assert.Equal(ExplanationSource.Ai, rec.ExplanationSource);
        Assert.Equal("You think like an engineer.", rec.Explanation);
        var sent = JsonSerializer.Serialize(Assert.Single(ai.Requests));
        Assert.DoesNotContain(id.ToString(), sent);
        Assert.DoesNotContain("Test Student", sent);
        Assert.DoesNotContain("@", sent);
        Assert.Equal(3, ai.Requests[0].Careers.Count);
    }

    [Fact]
    public async Task Recommendations_AiUnavailable_FallsBack()
    {
        await using var app = WithAi(factory, new FakeCareerAi(null));
        var id = NextId();
        await factory.AddStudentAsync(id);
        var student = await SignInAsync(app, id);
        var (_, answers) = await AnswerAllAsync(student, optionIndex: 3);
        await SubmitAsync(student, answers);

        var res = await student.GetAsync("/api/career/recommendations");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var rec = await res.ReadAsync<RecommendationsDto>();
        Assert.Equal(ExplanationSource.Fallback, rec.ExplanationSource);
        Assert.False(rec.UsesGrades); // a new student has no graded courses
        Assert.All(rec.Careers, c => Assert.Null(c.GradePct));
    }

    [Fact]
    public async Task Roadmap_ShowsDoneInProgressNextAndMilestones()
    {
        var student = await factory.ClientForAsync(SisApiFactory.StudentId);
        var careers = await (await student.GetAsync("/api/career/careers")).ReadAsync<List<CareerSummaryDto>>();
        var swe = careers.Single(c => c.Name == "Software Engineer");

        var roadmap = await (await student.GetAsync($"/api/career/careers/{swe.Id}/roadmap")).ReadAsync<RoadmapDto>();

        Assert.Equal(
            [("CSC 101", RoadmapStepStatus.Done), ("CSC 210", RoadmapStepStatus.InProgress), ("MAT 201", RoadmapStepStatus.InProgress),
             ("CSC 310", RoadmapStepStatus.Next), (null, RoadmapStepStatus.Milestone)],
            roadmap.Steps.Select(s => (s.Course?.Code, s.Status)));
        Assert.Equal((1, 4), (roadmap.DoneCount, roadmap.CourseStepCount));
        var csc310 = roadmap.Steps[3].Course!;
        Assert.Equal("A", csc310.Sections.Single().SectionCode);
        Assert.Equal("Enrolled", roadmap.Steps[1].Course!.Sections.Single(s => s.SectionCode == "A").Status);
    }

    [Fact]
    public async Task Roadmap_AfterAddToCart_ShowsInCart()
    {
        var id = NextId();
        var student = await NewStudentAsync(factory, id);
        var careers = await (await student.GetAsync("/api/career/careers")).ReadAsync<List<CareerSummaryDto>>();
        var dba = careers.Single(c => c.Name == "Database Administrator");
        var before = await (await student.GetAsync($"/api/career/careers/{dba.Id}/roadmap")).ReadAsync<RoadmapDto>();
        Assert.Equal(RoadmapStepStatus.Next, before.Steps[0].Status); // nothing passed yet: CSC 101 is next
        var section = before.Steps[0].Course!.Sections[0];

        Assert.Equal(HttpStatusCode.OK, (await student.PostAsJsonAsync("/api/cart", new { sectionId = section.SectionId })).StatusCode);

        var after = await (await student.GetAsync($"/api/career/careers/{dba.Id}/roadmap")).ReadAsync<RoadmapDto>();
        Assert.Equal("InCart", after.Steps[0].Course!.Sections[0].Status);
    }

    [Fact]
    public async Task Roadmap_UnknownCareer_404()
    {
        var student = await factory.ClientForAsync(SisApiFactory.StudentId);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync("/api/career/careers/999999/roadmap")).StatusCode);
    }
}
