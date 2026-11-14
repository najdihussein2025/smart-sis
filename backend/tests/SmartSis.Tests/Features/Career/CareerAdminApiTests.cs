using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Tests.Infrastructure;
using static SmartSis.Tests.Features.Career.CareerTestSupport;

namespace SmartSis.Tests.Features.Career;

/// <summary>PB-16 / T27, T28, T30 and PB-20 / T72, T74: the admin question bank, traits, careers and course tags.</summary>
public sealed class CareerAdminApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/career";
    private static int _next;
    private static string Unique(string prefix) => $"{prefix} {Interlocked.Increment(ref _next)}";

    private Task<HttpClient> AdminAsync() => factory.ClientForAsync(SisApiFactory.AdminId);

    private static async Task<IReadOnlyList<TraitDto>> TraitsAsync(HttpClient admin) =>
        await (await admin.GetAsync($"{Url}/traits")).ReadAsync<List<TraitDto>>();

    private static SaveQuestionRequest Question(int traitA, int traitB, string? text = null) => new(
        text ?? Unique("Which task would you enjoy most?"), true,
        [
            new SaveOptionRequest(null, "Solving puzzles", [new SaveTraitPointsRequest(traitA, 3)]),
            new SaveOptionRequest(null, "Drawing", [new SaveTraitPointsRequest(traitB, 2), new SaveTraitPointsRequest(traitA, 1)]),
        ]);

    private static string Code(ProblemDetails p) => p.Extensions["code"]!.ToString()!;

    [Fact]
    public async Task Bank_ListsSeededQuestions_WithCounts()
    {
        var admin = await AdminAsync();
        var bank = await (await admin.GetAsync($"{Url}/questions")).ReadAsync<QuestionBankDto>();

        Assert.True(bank.ActiveCount >= 24);
        Assert.Equal((20, 30), (bank.MinQuestions, bank.MaxQuestions));
        var first = bank.Questions[0];
        Assert.Equal(4, first.Options.Count);
        Assert.All(first.Options, o => Assert.All(o.Points, p => Assert.False(string.IsNullOrEmpty(p.TraitName))));
    }

    [Fact]
    public async Task Question_CreateGetUpdateDelete()
    {
        var admin = await AdminAsync();
        var traits = await TraitsAsync(admin);
        var (a, b) = (traits[0].Id, traits[1].Id);

        var created = await admin.PostAsJsonAsync($"{Url}/questions", Question(a, b));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var q = await created.ReadAsync<QuestionDto>();
        Assert.Equal(2, q.Options.Count);
        Assert.Equal(2, q.Options[1].Points.Count);
        Assert.Equal(q.Id, (await (await admin.GetAsync($"{Url}/questions/{q.Id}")).ReadAsync<QuestionDto>()).Id);

        // Keep option 1 (new text and points), drop option 2, add a new one; deactivate.
        var update = new SaveQuestionRequest("Updated question text", false,
        [
            new SaveOptionRequest(q.Options[0].Id, "Solving hard puzzles", [new SaveTraitPointsRequest(b, 5)]),
            new SaveOptionRequest(null, "Talking", [new SaveTraitPointsRequest(a, 1)]),
        ]);
        var updated = await (await admin.PutAsJsonAsync($"{Url}/questions/{q.Id}", update)).ReadAsync<QuestionDto>();
        Assert.False(updated.IsActive);
        Assert.Equal(q.Options[0].Id, updated.Options[0].Id);
        Assert.Equal(("Solving hard puzzles", b, 5), (updated.Options[0].Text, updated.Options[0].Points.Single().TraitId, updated.Options[0].Points.Single().Points));
        Assert.Equal("Talking", updated.Options[1].Text);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Url}/questions/{q.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Url}/questions/{q.Id}")).StatusCode);
    }

    [Fact]
    public async Task Question_Answered_CannotBeDeleted_OrLoseAChosenOption_ButCanBeDeactivated()
    {
        var student = await NewStudentAsync(factory, 3951);
        var (quiz, answers) = await AnswerAllAsync(student);
        Assert.Equal(HttpStatusCode.Created, (await SubmitAsync(student, answers)).StatusCode);

        var admin = await AdminAsync();
        var answeredId = quiz.Questions[0].Id;
        var del = await admin.DeleteAsync($"{Url}/questions/{answeredId}");
        Assert.Equal(HttpStatusCode.Conflict, del.StatusCode);
        Assert.Equal("question_answered", Code(await del.ReadAsync<ProblemDetails>()));

        var q = await (await admin.GetAsync($"{Url}/questions/{answeredId}")).ReadAsync<QuestionDto>();
        Assert.True(q.AnswerCount >= 1);
        SaveOptionRequest Keep(QuestionOptionDto o) =>
            new(o.Id, o.Text, o.Points.Select(p => new SaveTraitPointsRequest(p.TraitId, p.Points)).ToArray());

        // Removing the chosen first option → 409.
        var removeChosen = await admin.PutAsJsonAsync($"{Url}/questions/{answeredId}",
            new SaveQuestionRequest(q.Text, true, q.Options.Skip(1).Select(Keep).ToArray()));
        Assert.Equal(HttpStatusCode.Conflict, removeChosen.StatusCode);
        Assert.Equal("option_answered", Code(await removeChosen.ReadAsync<ProblemDetails>()));

        // Deactivating is allowed.
        var deactivate = await admin.PutAsJsonAsync($"{Url}/questions/{answeredId}",
            new SaveQuestionRequest(q.Text, false, q.Options.Select(Keep).ToArray()));
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        Assert.False((await deactivate.ReadAsync<QuestionDto>()).IsActive);

        // Put it back so the quiz keeps its seeded size for other tests.
        await admin.PutAsJsonAsync($"{Url}/questions/{answeredId}", new SaveQuestionRequest(q.Text, true, q.Options.Select(Keep).ToArray()));
    }

    [Fact]
    public async Task Question_Invalid_400()
    {
        var admin = await AdminAsync();
        var traits = await TraitsAsync(admin);

        var oneOption = new SaveQuestionRequest("Too short a list", true, [new SaveOptionRequest(null, "Only", [new SaveTraitPointsRequest(traits[0].Id, 3)])]);
        var res = await admin.PostAsJsonAsync($"{Url}/questions", oneOption);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("options", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        var badPoints = Question(traits[0].Id, traits[1].Id) with
        {
            Options = [new SaveOptionRequest(null, "A", [new SaveTraitPointsRequest(traits[0].Id, 9)]), new SaveOptionRequest(null, "B", [])],
        };
        res = await admin.PostAsJsonAsync($"{Url}/questions", badPoints);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("options[0].points[0].points", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        res = await admin.PostAsJsonAsync($"{Url}/questions", Question(traits[0].Id, 999_999));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("options[1].points[0].traitId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task Question_OptionOfAnotherQuestion_400()
    {
        var admin = await AdminAsync();
        var traits = await TraitsAsync(admin);
        var q1 = await (await admin.PostAsJsonAsync($"{Url}/questions", Question(traits[0].Id, traits[1].Id))).ReadAsync<QuestionDto>();
        var q2 = await (await admin.PostAsJsonAsync($"{Url}/questions", Question(traits[0].Id, traits[1].Id))).ReadAsync<QuestionDto>();

        var res = await admin.PutAsJsonAsync($"{Url}/questions/{q2.Id}", new SaveQuestionRequest(q2.Text, true,
        [
            new SaveOptionRequest(q1.Options[0].Id, "Stolen", [new SaveTraitPointsRequest(traits[0].Id, 1)]),
            new SaveOptionRequest(null, "New", []),
        ]));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("options[0].id", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        await admin.DeleteAsync($"{Url}/questions/{q1.Id}");
        await admin.DeleteAsync($"{Url}/questions/{q2.Id}");
    }

    [Fact]
    public async Task Traits_CreateRename_DuplicateIs409()
    {
        var admin = await AdminAsync();
        var name = Unique("Patience");
        var created = await admin.PostAsJsonAsync($"{Url}/traits", new SaveTraitRequest(name, "Staying calm."));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var trait = await created.ReadAsync<TraitDto>();

        var dup = await admin.PostAsJsonAsync($"{Url}/traits", new SaveTraitRequest(name.ToUpperInvariant(), null));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        Assert.Equal("duplicate_trait", Code(await dup.ReadAsync<ProblemDetails>()));

        var renamed = await (await admin.PutAsJsonAsync($"{Url}/traits/{trait.Id}", new SaveTraitRequest(name + "!", "  "))).ReadAsync<TraitDto>();
        Assert.Equal((name + "!", (string?)null), (renamed.Name, renamed.Description));

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync($"{Url}/traits", new SaveTraitRequest(" ", null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{Url}/traits/999999", new SaveTraitRequest("Ghost", null))).StatusCode);
    }

    [Fact]
    public async Task Careers_CreateWithWeightsAndRoadmap_ThenReplace()
    {
        var admin = await AdminAsync();
        var traits = await TraitsAsync(admin);
        var courses = await CoursesAsync();
        var name = Unique("Game Developer");

        var request = new SaveCareerPathRequest(name, "Makes games.", true,
            [new SaveTraitWeightRequest(traits[0].Id, 8), new SaveTraitWeightRequest(traits[1].Id, 3)],
            [
                new SaveRoadmapStepRequest("Learn to program", null, courses["CSC 101"]),
                new SaveRoadmapStepRequest("Data structures", "Trees and graphs.", courses["CSC 210"]),
                new SaveRoadmapStepRequest("Ship a game jam entry", null, null),
            ]);
        var created = await admin.PostAsJsonAsync($"{Url}/careers", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var career = await created.ReadAsync<CareerPathDto>();
        Assert.Equal([1, 2, 3], career.Steps.Select(s => s.StepOrder));
        Assert.Equal("CSC 101", career.Steps[0].CourseCode);
        Assert.Equal([8, 3], career.Traits.Select(t => t.Weight));

        // Replace: one trait with a new weight, two steps in a new order, inactive.
        var replaced = await (await admin.PutAsJsonAsync($"{Url}/careers/{career.Id}", request with
        {
            IsActive = false,
            Traits = [new SaveTraitWeightRequest(traits[1].Id, 10)],
            Steps = [new SaveRoadmapStepRequest("Discrete maths", null, courses["MAT 201"]), new SaveRoadmapStepRequest("Learn to program", null, courses["CSC 101"])],
        })).ReadAsync<CareerPathDto>();
        Assert.False(replaced.IsActive);
        Assert.Equal((traits[1].Id, 10), (replaced.Traits.Single().TraitId, replaced.Traits.Single().Weight));
        Assert.Equal(["MAT 201", "CSC 101"], replaced.Steps.Select(s => s.CourseCode));

        // Inactive careers are hidden from students.
        var student = await factory.ClientForAsync(SisApiFactory.StudentId);
        var visible = await (await student.GetAsync("/api/career/careers")).ReadAsync<List<CareerSummaryDto>>();
        Assert.DoesNotContain(visible, c => c.Id == career.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/career/careers/{career.Id}/roadmap")).StatusCode);
    }

    [Fact]
    public async Task Careers_DuplicateName409_UnknownReferences400()
    {
        var admin = await AdminAsync();
        var traits = await TraitsAsync(admin);

        var dup = await admin.PostAsJsonAsync($"{Url}/careers",
            new SaveCareerPathRequest("software engineer", null, true, [new SaveTraitWeightRequest(traits[0].Id, 5)], []));
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        Assert.Equal("duplicate_career", Code(await dup.ReadAsync<ProblemDetails>()));

        var bad = await admin.PostAsJsonAsync($"{Url}/careers",
            new SaveCareerPathRequest(Unique("Pilot"), null, true, [new SaveTraitWeightRequest(999_999, 5)], [new SaveRoadmapStepRequest("Fly", null, 999_999)]));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var errors = (await bad.ReadAsync<ValidationProblemDetails>()).Errors.Keys;
        Assert.Contains("traits[0].traitId", errors);
        Assert.Contains("steps[0].courseId", errors);

        var noTraits = await admin.PostAsJsonAsync($"{Url}/careers", new SaveCareerPathRequest(Unique("Pilot"), null, true, [], []));
        Assert.Equal(HttpStatusCode.BadRequest, noTraits.StatusCode);
    }

    [Fact]
    public async Task CourseTags_ListAndReplace()
    {
        var admin = await AdminAsync();
        var traits = await TraitsAsync(admin);
        var tags = await (await admin.GetAsync($"{Url}/course-tags")).ReadAsync<List<CourseTagsDto>>();
        var csc310 = tags.Single(t => t.Code == "CSC 310");
        Assert.Equal(2, csc310.TraitIds.Count); // seeded: attention to detail + analytical

        var saved = await (await admin.PutAsJsonAsync($"{Url}/course-tags/{csc310.CourseId}",
            new SaveCourseTagsRequest([traits[0].Id]))).ReadAsync<CourseTagsDto>();
        Assert.Equal([traits[0].Id], saved.TraitIds);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{Url}/course-tags/999999", new SaveCourseTagsRequest([]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsJsonAsync($"{Url}/course-tags/{csc310.CourseId}", new SaveCourseTagsRequest([999_999]))).StatusCode);

        // Put the seeded tags back.
        await admin.PutAsJsonAsync($"{Url}/course-tags/{csc310.CourseId}", new SaveCourseTagsRequest([.. csc310.TraitIds]));
    }

    private async Task<Dictionary<string, int>> CoursesAsync()
    {
        Dictionary<string, int> map = [];
        await factory.WithDbAsync(async db => map = await db.Set<Course>().ToDictionaryAsync(c => c.Code, c => c.Id));
        return map;
    }
}
