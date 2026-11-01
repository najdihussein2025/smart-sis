using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Terms.Models.Dtos;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Terms.Validators;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Terms;

/// <summary>Admin terms API (PB-02, UC-16). Each test uses its own term names, so order doesn't matter.</summary>
[Trait("SR", "SR-21")]
public sealed class TermsApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/terms";
    private static readonly DateTime Base = new(2027, 1, 10, 8, 0, 0, DateTimeKind.Utc);

    private static string UniqueName(string prefix = "Term") => $"{prefix} {Guid.NewGuid():N}"[..30];

    /// <summary>A valid term: Jan 15 – May 31 2027, enrollment Jan 10–17, add/drop Jan 17–24.</summary>
    private static SaveTermRequest Valid(string name, bool isCurrent = false) => new(
        name,
        new DateOnly(2027, 1, 15), new DateOnly(2027, 5, 31),
        Base, Base.AddDays(7), Base.AddDays(7), Base.AddDays(14),
        isCurrent);

    private Task<HttpClient> AdminAsync() => factory.ClientForAsync(SisApiFactory.AdminId);

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage res) =>
        JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

    [Fact]
    public async Task Create_Valid_Returns201WithLocationAndIsReadable()
    {
        var admin = await AdminAsync();
        var name = UniqueName("Spring");

        var res = await admin.PostAsJsonAsync(Url, Valid("  " + name + "  "));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.Content.ReadFromJsonAsync<TermDto>();
        Assert.Equal(name, created!.Name); // trimmed
        Assert.Equal($"{Url}/{created.Id}", res.Headers.Location!.AbsolutePath, ignoreCase: true);

        var fetched = await admin.GetFromJsonAsync<TermDto>(res.Headers.Location);
        Assert.Equal(created, fetched);
        Assert.Equal(Base, fetched!.EnrollStart);
        Assert.Equal(DateTimeKind.Utc, fetched.EnrollStart.Kind);
    }

    [Fact]
    public async Task Update_Valid_Persists()
    {
        var admin = await AdminAsync();
        var created = await (await admin.PostAsJsonAsync(Url, Valid(UniqueName()))).Content.ReadFromJsonAsync<TermDto>();
        var newName = UniqueName("Renamed");

        var res = await admin.PutAsJsonAsync($"{Url}/{created!.Id}", Valid(newName) with { EndDate = new DateOnly(2027, 6, 15) });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var fetched = await admin.GetFromJsonAsync<TermDto>($"{Url}/{created.Id}");
        Assert.Equal(newName, fetched!.Name);
        Assert.Equal(new DateOnly(2027, 6, 15), fetched.EndDate);
    }

    [Fact]
    public async Task Update_KeepsOwnName_Returns200()
    {
        var admin = await AdminAsync();
        var name = UniqueName();
        var created = await (await admin.PostAsJsonAsync(Url, Valid(name))).Content.ReadFromJsonAsync<TermDto>();

        var res = await admin.PutAsJsonAsync($"{Url}/{created!.Id}", Valid(name.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateNameDifferentCase_Returns409()
    {
        var admin = await AdminAsync();
        var name = UniqueName();
        await admin.PostAsJsonAsync(Url, Valid(name));

        var res = await admin.PostAsJsonAsync(Url, Valid(name.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await ProblemAsync(res);
        Assert.Equal("duplicate_term_name", problem.GetProperty("code").GetString());
        Assert.Equal($"A term named '{name.ToUpperInvariant()}' already exists.", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Update_NameTakenByAnotherTerm_Returns409()
    {
        var admin = await AdminAsync();
        var taken = UniqueName();
        await admin.PostAsJsonAsync(Url, Valid(taken));
        var other = await (await admin.PostAsJsonAsync(Url, Valid(UniqueName()))).Content.ReadFromJsonAsync<TermDto>();

        var res = await admin.PutAsJsonAsync($"{Url}/{other!.Id}", Valid(taken));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_term_name", (await ProblemAsync(res)).GetProperty("code").GetString());
    }

    public static TheoryData<string, string> InvalidRequests => new()
    {
        { "blank-name", "name" },
        { "long-name", "name" },
        { "end-before-start", "endDate" },
        { "enroll-reversed", "enrollEnd" },
        { "adddrop-reversed", "addDropEnd" },
        { "adddrop-before-enroll-ends", "addDropStart" },
        { "adddrop-after-term", "addDropEnd" },
    };

    private static SaveTermRequest Broken(string kind)
    {
        var r = Valid(UniqueName());
        return kind switch
        {
            "blank-name" => r with { Name = "   " },
            "long-name" => r with { Name = new string('x', 65) },
            "end-before-start" => r with { EndDate = r.StartDate },
            "enroll-reversed" => r with { EnrollEnd = r.EnrollStart },
            "adddrop-reversed" => r with { AddDropEnd = r.AddDropStart },
            "adddrop-before-enroll-ends" => r with { AddDropStart = r.EnrollEnd.AddHours(-1) },
            "adddrop-after-term" => r with { AddDropEnd = r.EndDate.AddDays(1).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc) },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_BrokenRule_Returns400OnField(string kind, string field)
    {
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Broken(kind));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True((await ProblemAsync(res)).GetProperty("errors").TryGetProperty(field, out _), $"expected an error on '{field}'");
    }

    [Theory]
    [InlineData("enrollStart")]
    [InlineData("enrollEnd")]
    [InlineData("addDropStart")]
    [InlineData("addDropEnd")]
    public async Task Create_WindowWithoutZ_Returns400Utc(string field)
    {
        // Raw JSON so the timestamp has no offset (deserializes as DateTimeKind.Unspecified).
        var body = new Dictionary<string, object>
        {
            ["name"] = UniqueName(), ["startDate"] = "2027-01-15", ["endDate"] = "2027-05-31",
            ["enrollStart"] = "2027-01-10T08:00:00Z", ["enrollEnd"] = "2027-01-17T08:00:00Z",
            ["addDropStart"] = "2027-01-17T08:00:00Z", ["addDropEnd"] = "2027-01-24T08:00:00Z",
            ["isCurrent"] = false,
        };
        body[field] = ((string)body[field]).TrimEnd('Z');

        var res = await (await AdminAsync()).PostAsJsonAsync(Url, body);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var errors = (await ProblemAsync(res)).GetProperty("errors").GetProperty(field);
        Assert.Contains(SaveTermRequestValidator.UtcMessage, errors.EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Create_EnrollEndEqualsAddDropStart_IsAllowed()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Valid(UniqueName()));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_AddDropEndsOnLastDayOfTerm_IsAllowed()
    {
        var r = Valid(UniqueName());
        var res = await (await AdminAsync()).PostAsJsonAsync(Url,
            r with { AddDropEnd = r.EndDate.ToDateTime(new TimeOnly(23, 0), DateTimeKind.Utc) });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_IsCurrent_ClearsPreviousCurrentTerm()
    {
        var admin = await AdminAsync();

        var first = await (await admin.PostAsJsonAsync(Url, Valid(UniqueName(), isCurrent: true))).Content.ReadFromJsonAsync<TermDto>();
        var second = await (await admin.PostAsJsonAsync(Url, Valid(UniqueName(), isCurrent: true))).Content.ReadFromJsonAsync<TermDto>();

        var current = (await admin.GetFromJsonAsync<TermDto[]>(Url))!.Where(t => t.IsCurrent).ToList();
        Assert.Equal(second!.Id, Assert.Single(current).Id);
        Assert.False((await admin.GetFromJsonAsync<TermDto>($"{Url}/{first!.Id}"))!.IsCurrent);
        await factory.WithDbAsync(async db => Assert.Equal(1, await db.Set<Term>().CountAsync(t => t.IsCurrent)));
    }

    [Fact]
    public async Task Update_SetIsCurrent_ClearsPreviousAndUnsetLeavesNone()
    {
        var admin = await AdminAsync();
        var term = await (await admin.PostAsJsonAsync(Url, Valid(UniqueName()))).Content.ReadFromJsonAsync<TermDto>();

        var set = await admin.PutAsJsonAsync($"{Url}/{term!.Id}", Valid(term.Name, isCurrent: true));
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        await factory.WithDbAsync(async db =>
            Assert.Equal(term.Id, (await db.Set<Term>().SingleAsync(t => t.IsCurrent)).Id));

        // Saving the current term again as current keeps it current.
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{Url}/{term.Id}", Valid(term.Name, isCurrent: true))).StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(1, await db.Set<Term>().CountAsync(t => t.IsCurrent)));

        var unset = await admin.PutAsJsonAsync($"{Url}/{term.Id}", Valid(term.Name, isCurrent: false));
        Assert.Equal(HttpStatusCode.OK, unset.StatusCode);
        await factory.WithDbAsync(async db => Assert.Equal(0, await db.Set<Term>().CountAsync(t => t.IsCurrent)));

        // Restore a current term so other tests see the usual state.
        await admin.PutAsJsonAsync($"{Url}/{term.Id}", Valid(term.Name, isCurrent: true));
    }

    [Fact]
    public async Task List_NewestStartDateFirst()
    {
        var admin = await AdminAsync();
        var older = Valid(UniqueName("Old")) with { StartDate = new DateOnly(2027, 1, 14) };
        var newer = Valid(UniqueName("New")) with { StartDate = new DateOnly(2027, 1, 15) };
        await admin.PostAsJsonAsync(Url, older);
        await admin.PostAsJsonAsync(Url, newer);

        var list = (await admin.GetFromJsonAsync<TermDto[]>(Url))!;

        Assert.Equal(list.OrderByDescending(t => t.StartDate).Select(t => t.StartDate), list.Select(t => t.StartDate));
        Assert.True(Array.FindIndex(list, t => t.Name == newer.Name) < Array.FindIndex(list, t => t.Name == older.Name));
    }

    [Fact]
    public async Task Get_PastTerm_CurrentWindowClosed()
    {
        var past = new DateTime(2020, 1, 10, 8, 0, 0, DateTimeKind.Utc);
        var r = new SaveTermRequest(UniqueName("Past"), new DateOnly(2020, 1, 15), new DateOnly(2020, 5, 31),
            past, past.AddDays(7), past.AddDays(7), past.AddDays(14), false);

        var created = await (await (await AdminAsync()).PostAsJsonAsync(Url, r)).Content.ReadFromJsonAsync<TermDto>();

        Assert.Equal("Closed", created!.CurrentWindow);
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
        var res = await (await AdminAsync()).PutAsJsonAsync($"{Url}/999999", Valid(UniqueName()));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
