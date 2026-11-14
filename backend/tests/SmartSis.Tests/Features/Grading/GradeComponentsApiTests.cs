using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Grading;

/// <summary>T31: admin grade components per course per term (rule G1).</summary>
public sealed class GradeComponentsApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/grade-components";

    private async Task<(HttpClient Admin, int CourseId, int TermId)> SetupAsync()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        return (admin, await factory.CourseIdAsync("MAT 201"), await factory.NewTermAsync());
    }

    private static async Task<GradeComponentDto> CreateAsync(HttpClient admin, int courseId, int termId, string name, decimal max, decimal weight)
    {
        var res = await admin.PostAsJsonAsync(Url, new CreateGradeComponentRequest(courseId, termId, name, max, weight));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await res.ReadAsync<GradeComponentDto>();
    }

    [Fact]
    public async Task Create_ThenList_ShowsComponentsAndTotal()
    {
        var (admin, courseId, termId) = await SetupAsync();
        await CreateAsync(admin, courseId, termId, "  Midterm ", 50, 30);
        await CreateAsync(admin, courseId, termId, "Final", 100, 70);

        var list = await (await admin.GetAsync($"{Url}?courseId={courseId}&termId={termId}")).ReadAsync<GradeComponentListDto>();

        Assert.Equal(100, list.TotalWeight);
        Assert.False(list.IsFinalized);
        Assert.Equal(["Midterm", "Final"], list.Items.Select(i => i.Name));
        Assert.Equal(50, list.Items[0].MaxGrade);
        Assert.All(list.Items, i => Assert.Equal(0, i.ScoreCount));
    }

    [Fact]
    [Trait("Rule", "G1")]
    public async Task Create_WeightsOver100_Returns409()
    {
        var (admin, courseId, termId) = await SetupAsync();
        await CreateAsync(admin, courseId, termId, "Midterm", 50, 60);

        var res = await admin.PostAsJsonAsync(Url, new CreateGradeComponentRequest(courseId, termId, "Final", 100, 41));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.ReadAsync<ProblemDetails>();
        Assert.Equal("weight_over_100", problem.Extensions["code"]!.ToString());
        Assert.Contains("40% is left", problem.Title);
    }

    [Fact]
    [Trait("Rule", "G1")]
    public async Task Update_WeightsOver100_Returns409_AndOwnWeightIsLeftOut()
    {
        var (admin, courseId, termId) = await SetupAsync();
        var midterm = await CreateAsync(admin, courseId, termId, "Midterm", 50, 40);
        await CreateAsync(admin, courseId, termId, "Final", 100, 60);

        var ok = await admin.PutAsJsonAsync($"{Url}/{midterm.Id}", new UpdateGradeComponentRequest("Midterm exam", 60, 40));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var updated = await ok.ReadAsync<GradeComponentDto>();
        Assert.Equal(("Midterm exam", 60m), (updated.Name, updated.MaxGrade));

        var tooMuch = await admin.PutAsJsonAsync($"{Url}/{midterm.Id}", new UpdateGradeComponentRequest("Midterm exam", 60, 41));
        Assert.Equal(HttpStatusCode.Conflict, tooMuch.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        var (admin, courseId, termId) = await SetupAsync();
        await CreateAsync(admin, courseId, termId, "Quiz", 10, 10);

        var res = await admin.PostAsJsonAsync(Url, new CreateGradeComponentRequest(courseId, termId, "quiz", 10, 10));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_component_name", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]!.ToString());
    }

    [Fact]
    public async Task Create_InvalidFields_Returns400WithFieldErrors()
    {
        var (admin, courseId, termId) = await SetupAsync();

        var res = await admin.PostAsJsonAsync(Url, new CreateGradeComponentRequest(courseId, termId, " ", 0, 100.5m));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("name", problem.Errors.Keys);
        Assert.Contains("maxGrade", problem.Errors.Keys);
        Assert.Contains("weight", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_UnknownCourse_Returns400()
    {
        var (admin, _, termId) = await SetupAsync();
        var res = await admin.PostAsJsonAsync(Url, new CreateGradeComponentRequest(999999, termId, "Final", 100, 50));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("courseId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task List_WithoutCourseAndTerm_Returns400()
    {
        var (admin, _, _) = await SetupAsync();
        var res = await admin.GetAsync(Url);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Delete_WithoutScores_Returns204()
    {
        var (admin, courseId, termId) = await SetupAsync();
        var quiz = await CreateAsync(admin, courseId, termId, "Quiz", 10, 10);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Url}/{quiz.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Url}/{quiz.Id}")).StatusCode);
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        var (admin, _, _) = await SetupAsync();
        var res = await admin.PutAsJsonAsync($"{Url}/999999", new UpdateGradeComponentRequest("X", 10, 10));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
