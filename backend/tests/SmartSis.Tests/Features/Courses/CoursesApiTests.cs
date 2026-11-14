using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Courses;

/// <summary>Admin courses API (PB-02, UC-16). Each test creates its own course codes, so order doesn't matter.</summary>
[Trait("SR", "SR-21")]
public sealed class CoursesApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/courses";

    /// <summary>The API sends enums as strings; the test client must read them the same way.</summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static int _next = 100;

    /// <summary>A code nobody else in this class uses: "TA 100", "TA 101", …, rolling into "TB …" etc.</summary>
    private static string UniqueCode()
    {
        var n = Interlocked.Increment(ref _next);
        return $"T{(char)('A' + n / 1000 % 26)} {n % 1000:000}";
    }

    private Task<HttpClient> AdminAsync() => factory.ClientForAsync(SisApiFactory.AdminId);

    private async Task<(int CscDept, int MatDept, int CsMajor, Dictionary<string, int> Courses)> SeedAsync()
    {
        var result = default((int, int, int, Dictionary<string, int>));
        await factory.WithDbAsync(async db =>
        {
            var depts = await db.Set<Department>().ToDictionaryAsync(d => d.Code, d => d.Id);
            var major = await db.Set<Major>().SingleAsync(m => m.Name == "Computer Science");
            var courses = await db.Set<Course>().ToDictionaryAsync(c => c.Code, c => c.Id);
            result = (depts["CSC"], depts["MAT"], major.Id, courses);
        });
        return result;
    }

    private async Task<int> AddMajorAsync(string name)
    {
        var id = 0;
        await factory.WithDbAsync(async db =>
        {
            var major = new Major { DepartmentId = (await db.Set<Department>().FirstAsync()).Id, Name = name };
            db.Add(major);
            await db.SaveChangesAsync();
            id = major.Id;
        });
        return id;
    }

    private static SaveCourseRequest Request(string code, int departmentId, int[]? prerequisites = null, CourseMajorRequest[]? majors = null) =>
        new(code, "Test course " + code, 3, "Simulated course for tests.", departmentId, prerequisites ?? [], majors ?? []);

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage res) =>
        JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await ProblemAsync(res)).TryGetProperty("code", out var c) ? c.GetString() : null;

    [Fact]
    public async Task Create_WithPrerequisitesAndMajors_DetailShowsThem()
    {
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var code = UniqueCode();

        var res = await admin.PostAsJsonAsync(Url, Request(code, seed.CscDept,
            [seed.Courses["CSC 210"], seed.Courses["CSC 101"]],
            [new(seed.CsMajor, CourseType.Elective)]));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.Content.ReadFromJsonAsync<CourseDetailDto>(Json);
        Assert.Equal($"{Url}/{created!.Id}", res.Headers.Location!.AbsolutePath, ignoreCase: true);

        var detail = await admin.GetFromJsonAsync<CourseDetailDto>(res.Headers.Location, Json);
        Assert.Equal(code, detail!.Code);
        Assert.Equal("Computer Science", detail.DepartmentName);
        Assert.Equal(["CSC 101", "CSC 210"], detail.Prerequisites.Select(p => p.Code)); // ordered by code
        var major = Assert.Single(detail.Majors);
        Assert.Equal((seed.CsMajor, "Computer Science", CourseType.Elective), (major.MajorId, major.MajorName, major.Type));
    }

    [Fact]
    public async Task Create_TypeIsSentAndReturnedAsString()
    {
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var body = $$"""
            { "code": "{{UniqueCode()}}", "name": "String enum", "credits": 3, "description": null,
              "departmentId": {{seed.CscDept}}, "prerequisiteIds": [], "majors": [ { "majorId": {{seed.CsMajor}}, "type": "Elective" } ] }
            """;

        var res = await admin.PostAsync(Url, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Contains("\"type\":\"Elective\"", await res.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("tq101", "TQ 101")]
    [InlineData("Tq 102a", "TQ 102A")]
    public async Task Create_CodeIsNormalized(string input, string expected)
    {
        var seed = await SeedAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(input, seed.CscDept));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal(expected, (await res.Content.ReadFromJsonAsync<CourseDetailDto>(Json))!.Code);
    }

    [Theory]
    [InlineData("csc 101")]
    [InlineData("CSC101")]
    [InlineData("CSC 101")]
    public async Task Create_DuplicateOfSeededCode_Returns409(string code)
    {
        var seed = await SeedAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(code, seed.CscDept));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_course_code", await CodeOf(res));
        Assert.Equal("Course code CSC 101 already exists.", (await ProblemAsync(res)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Update_CodeTakenByAnotherCourse_Returns409()
    {
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var created = await (await admin.PostAsJsonAsync(Url, Request(UniqueCode(), seed.CscDept))).Content.ReadFromJsonAsync<CourseDetailDto>(Json);

        var res = await admin.PutAsJsonAsync($"{Url}/{created!.Id}", Request("mat201", seed.MatDept));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_course_code", await CodeOf(res));
    }

    [Fact]
    public async Task Create_UnknownReferences_Returns400OnFields()
    {
        var seed = await SeedAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(UniqueCode(), 99999,
            [seed.Courses["CSC 101"], 88888],
            [new(seed.CsMajor, CourseType.Major), new(77777, CourseType.Elective)]));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var errors = (await ProblemAsync(res)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("departmentId", out _));
        Assert.True(errors.TryGetProperty("prerequisiteIds[1]", out _));
        Assert.True(errors.TryGetProperty("majors[1].majorId", out _));
        Assert.False(errors.TryGetProperty("prerequisiteIds[0]", out _));
    }

    public static TheoryData<string, string> InvalidRequests => new()
    {
        { "bad-code", "code" },
        { "blank-name", "name" },
        { "long-name", "name" },
        { "long-description", "description" },
        { "credits-negative", "credits" },
        { "credits-13", "credits" },
        { "duplicate-prerequisite", "prerequisiteIds" },
        { "duplicate-major", "majors" },
        { "missing-lists", "prerequisiteIds" },
        { "bad-type", "majors[0].type" },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_InvalidField_Returns400(string kind, string field)
    {
        var seed = await SeedAsync();
        var r = Request(UniqueCode(), seed.CscDept);
        r = kind switch
        {
            "bad-code" => r with { Code = "CSC-101" },
            "blank-name" => r with { Name = " " },
            "long-name" => r with { Name = new string('x', 151) },
            "long-description" => r with { Description = new string('x', 4001) },
            "credits-negative" => r with { Credits = -1 },
            "credits-13" => r with { Credits = 13 },
            "duplicate-prerequisite" => r with { PrerequisiteIds = [seed.Courses["CSC 101"], seed.Courses["CSC 101"]] },
            "duplicate-major" => r with { Majors = [new(seed.CsMajor, CourseType.Major), new(seed.CsMajor, CourseType.Elective)] },
            "missing-lists" => r with { PrerequisiteIds = null!, Majors = null! },
            "bad-type" => r with { Majors = [new(seed.CsMajor, (CourseType)7)] },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        // Missing lists are rejected by model binding before the validator runs; keys still match the validator's.
        var keys = (await ProblemAsync(res)).GetProperty("errors").EnumerateObject().Select(p => p.Name);
        Assert.Contains(field, keys, StringComparer.Ordinal);
    }

    // Raw JSON so the body can omit a field or carry an unknown enum name; both fail during model binding.
    public static TheoryData<string, string, string> InvalidJsonBodies => new()
    {
        { "missing-prerequisiteIds", """ "majors": [] """, "prerequisiteIds" },
        { "unknown-type", """ "prerequisiteIds": [], "majors": [ { "majorId": 1, "type": "Foo" } ] """, "majors[0].type" },
    };

    [Theory]
    [MemberData(nameof(InvalidJsonBodies))]
    public async Task Create_InvalidJsonBody_Returns400InValidatorShape(string kind, string lists, string field)
    {
        _ = kind;
        var seed = await SeedAsync();
        var body = $$"""
            { "code": "{{UniqueCode()}}", "name": "Bad body", "credits": 3, "description": null,
              "departmentId": {{seed.CscDept}}, {{lists}} }
            """;

        var res = await (await AdminAsync()).PostAsync(Url, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await ProblemAsync(res);
        Assert.Equal("Some fields are not valid.", problem.GetProperty("title").GetString());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
        var keys = problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Contains(field, keys, StringComparer.Ordinal);
        Assert.DoesNotContain("request", keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400InValidatorShape()
    {
        var res = await (await AdminAsync()).PostAsync(Url, new StringContent("{ \"code\": ", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await ProblemAsync(res);
        Assert.Equal("Some fields are not valid.", problem.GetProperty("title").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
        Assert.DoesNotContain(problem.GetProperty("errors").EnumerateObject(), p => p.Name.StartsWith('$'));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public async Task Create_CreditsAtBounds_IsAllowed(int credits)
    {
        var seed = await SeedAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(UniqueCode(), seed.CscDept) with { Credits = credits });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesPrerequisitesAndMajors()
    {
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var otherMajor = await AddMajorAsync("Simulated Major " + Guid.NewGuid().ToString("N")[..6]);
        var code = UniqueCode();
        var created = await (await admin.PostAsJsonAsync(Url, Request(code, seed.CscDept,
            [seed.Courses["CSC 101"], seed.Courses["CSC 210"]],
            [new(seed.CsMajor, CourseType.Major)]))).Content.ReadFromJsonAsync<CourseDetailDto>(Json);

        // Keep CSC 210, drop CSC 101, add MAT 201; change CS to Elective, add the other major.
        var res = await admin.PutAsJsonAsync($"{Url}/{created!.Id}", Request(code, seed.MatDept,
            [seed.Courses["CSC 210"], seed.Courses["MAT 201"]],
            [new(seed.CsMajor, CourseType.Elective), new(otherMajor, CourseType.Major)]) with { Name = "Renamed", Credits = 4 });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await admin.GetFromJsonAsync<CourseDetailDto>($"{Url}/{created.Id}", Json);
        Assert.Equal(("Renamed", 4, seed.MatDept), (detail!.Name, detail.Credits, detail.DepartmentId));
        Assert.Equal(["CSC 210", "MAT 201"], detail.Prerequisites.Select(p => p.Code));
        Assert.Equal(CourseType.Elective, detail.Majors.Single(m => m.MajorId == seed.CsMajor).Type);
        Assert.Equal(CourseType.Major, detail.Majors.Single(m => m.MajorId == otherMajor).Type);

        await factory.WithDbAsync(async db =>
        {
            Assert.Equal(2, await db.Set<CoursePrerequisite>().CountAsync(p => p.CourseId == created.Id));
            Assert.Equal(2, await db.Set<MajorCourse>().CountAsync(m => m.CourseId == created.Id));
        });

        // Empty lists clear everything.
        var cleared = await admin.PutAsJsonAsync($"{Url}/{created.Id}", Request(code, seed.MatDept));
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var empty = await cleared.Content.ReadFromJsonAsync<CourseDetailDto>(Json);
        Assert.Empty(empty!.Prerequisites);
        Assert.Empty(empty.Majors);
    }

    [Fact]
    public async Task Update_MakeCsc101RequireCsc310_Returns409Cycle()
    {
        var seed = await SeedAsync();
        var csc101 = seed.Courses["CSC 101"];

        var res = await (await AdminAsync()).PutAsJsonAsync($"{Url}/{csc101}",
            new SaveCourseRequest("CSC 101", "Introduction to Programming", 3, null, seed.CscDept,
                [seed.Courses["CSC 310"]], [new(seed.CsMajor, CourseType.Major)]));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("prerequisite_cycle", await CodeOf(res));
        Assert.Equal("CSC 101 can't require CSC 310: CSC 310 already depends on CSC 101 (CSC 310 → CSC 210 → CSC 101).",
            (await ProblemAsync(res)).GetProperty("title").GetString());
        await factory.WithDbAsync(async db =>
            Assert.Equal(0, await db.Set<CoursePrerequisite>().CountAsync(p => p.CourseId == csc101)));
    }

    [Fact]
    public async Task Update_DirectCycle_Returns409Cycle()
    {
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var a = await (await admin.PostAsJsonAsync(Url, Request(UniqueCode(), seed.CscDept))).Content.ReadFromJsonAsync<CourseDetailDto>(Json);
        var bCode = UniqueCode();
        await admin.PostAsJsonAsync(Url, Request(bCode, seed.CscDept, [a!.Id]));
        var b = (await SeedAsync()).Courses[bCode];

        var res = await admin.PutAsJsonAsync($"{Url}/{a.Id}", Request(a.Code, seed.CscDept, [b]));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal($"{a.Code} can't require {bCode}: {bCode} already depends on {a.Code}.", (await ProblemAsync(res)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Update_OwnPrerequisite_Returns409Self()
    {
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var created = await (await admin.PostAsJsonAsync(Url, Request(UniqueCode(), seed.CscDept))).Content.ReadFromJsonAsync<CourseDetailDto>(Json);

        var res = await admin.PutAsJsonAsync($"{Url}/{created!.Id}", Request(created.Code, seed.CscDept, [created.Id]));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("prerequisite_self", await CodeOf(res));
    }

    [Fact]
    public async Task Update_DiamondPrerequisites_IsAllowed()
    {
        // New course requires CSC 210 and CSC 101; CSC 210 already requires CSC 101. Not a cycle.
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var created = await (await admin.PostAsJsonAsync(Url, Request(UniqueCode(), seed.CscDept))).Content.ReadFromJsonAsync<CourseDetailDto>(Json);

        var res = await admin.PutAsJsonAsync($"{Url}/{created!.Id}",
            Request(created.Code, seed.CscDept, [seed.Courses["CSC 210"], seed.Courses["CSC 101"]]));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task List_SearchesCodeAndNameAndPages()
    {
        var admin = await AdminAsync();
        var seed = await SeedAsync();
        var tag = "Zq" + Guid.NewGuid().ToString("N")[..6];
        var codes = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var code = UniqueCode();
            codes.Add(code);
            await admin.PostAsJsonAsync(Url, Request(code, seed.MatDept) with { Name = $"{tag} course {i}" });
        }

        var page1 = await admin.GetFromJsonAsync<PagedResult<CourseListItemDto>>($"{Url}?search={tag.ToUpperInvariant()}&page=1&pageSize=2");
        var page2 = await admin.GetFromJsonAsync<PagedResult<CourseListItemDto>>($"{Url}?search={tag}&page=2&pageSize=2");

        Assert.Equal(3, page1!.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(codes.Order().Take(2), page1.Items.Select(c => c.Code));
        Assert.Equal(codes.Order().Skip(2), page2!.Items.Select(c => c.Code));
        Assert.All(page1.Items, c => Assert.Equal(("MAT", "Mathematics"), (c.DepartmentCode, c.DepartmentName)));

        var byCode = await admin.GetFromJsonAsync<PagedResult<CourseListItemDto>>($"{Url}?search=csc 2");
        Assert.Equal(["CSC 210"], byCode!.Items.Select(c => c.Code));
    }

    [Fact]
    public async Task List_FiltersByDepartment_OrderedByCode()
    {
        var seed = await SeedAsync();
        var list = await (await AdminAsync()).GetFromJsonAsync<PagedResult<CourseListItemDto>>($"{Url}?departmentId={seed.CscDept}&pageSize=100");

        Assert.All(list!.Items, c => Assert.Equal("CSC", c.DepartmentCode));
        Assert.Equal(list.Items.Select(c => c.Code).Order(StringComparer.Ordinal), list.Items.Select(c => c.Code));
        Assert.Contains("CSC 101", list.Items.Select(c => c.Code));
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var res = await (await AdminAsync()).GetAsync($"{Url}/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var seed = await SeedAsync();
        var res = await (await AdminAsync()).PutAsJsonAsync($"{Url}/999999", Request(UniqueCode(), seed.CscDept));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
