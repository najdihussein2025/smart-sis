using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Auth.Models.Dtos;
using SmartSis.Api.Features.Auth.Services;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Features.Users.Validators;
using SmartSis.Api.Shared.Models;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Users;

/// <summary>Admin users API (PB-03, UC-15): T09 add / edit, T10 validation and deactivation.</summary>
[Trait("SR", "SR-20")]
public sealed class UsersAdminApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/users";
    private const string Password = "Initial123";
    private static long _nextId = 500_000;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    /// <summary>A University ID (and matching simulated email) no other test in this class uses.</summary>
    private static long NewId() => Interlocked.Increment(ref _nextId);
    private static string EmailFor(long id) => $"user{id}@test.smartsis.local";

    private Task<HttpClient> AdminAsync() => factory.ClientForAsync(SisApiFactory.AdminId);

    private async Task<(int Major, int Campus, int CscDept)> LookupsAsync()
    {
        var r = default((int, int, int));
        await factory.WithDbAsync(async db => r = (
            (await db.Set<Major>().SingleAsync(m => m.Name == "Computer Science")).Id,
            (await db.Set<Campus>().SingleAsync(c => c.Name == "Main Campus")).Id,
            (await db.Set<Department>().SingleAsync(d => d.Code == "CSC")).Id));
        return r;
    }

    private async Task<CreateUserRequest> StudentAsync(long? id = null, string? email = null)
    {
        var l = await LookupsAsync();
        var uid = id ?? NewId();
        return new CreateUserRequest(uid, email ?? EmailFor(uid), $"Simulated Student {uid}", "+961 1 000000",
            new DateOnly(2004, 5, 1), UserRole.Student, Password,
            new StudentInfoRequest(l.Major, l.Campus, new DateOnly(2025, 9, 1), null), null);
    }

    private async Task<CreateUserRequest> InstructorAsync()
    {
        var l = await LookupsAsync();
        var uid = NewId();
        return new CreateUserRequest(uid, EmailFor(uid), $"Simulated Instructor {uid}", null, null,
            UserRole.Instructor, Password, null, new InstructorInfoRequest(l.CscDept));
    }

    private static UpdateUserRequest ToUpdate(CreateUserRequest r) =>
        new(r.Email, r.FullName, r.Phone, r.DateOfBirth, r.Student, r.Instructor);

    private async Task<UserDetailDto> CreateAsync(HttpClient admin, CreateUserRequest r)
    {
        var res = await admin.PostAsJsonAsync(Url, r, Json);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<UserDetailDto>(Json))!;
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage res) =>
        JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await ProblemAsync(res)).TryGetProperty("code", out var c) ? c.GetString() : null;

    private static async Task<bool> HasError(HttpResponseMessage res, string field) =>
        (await ProblemAsync(res)).GetProperty("errors").TryGetProperty(field, out _);

    private Task<HttpResponseMessage> LoginAsync(long uid, string password) =>
        factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(uid, password));

    // ───── create ─────

    [Fact]
    public async Task Create_Student_Returns201WithDetail()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();

        var res = await admin.PostAsJsonAsync(Url, r with { Email = "  " + r.Email.ToUpperInvariant() + " " }, Json);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = (await res.Content.ReadFromJsonAsync<UserDetailDto>(Json))!;
        Assert.Equal($"{Url}/{created.Id}", res.Headers.Location!.AbsolutePath, ignoreCase: true);

        var detail = (await admin.GetFromJsonAsync<UserDetailDto>(res.Headers.Location, Json))!;
        Assert.Equal((r.UniversityId, r.Email, UserRole.Student, true), (detail.UniversityId, detail.Email, detail.Role, detail.IsActive));
        Assert.Null(detail.DeletedAt);
        Assert.Null(detail.Instructor);
        Assert.Equal(("Computer Science", "Main Campus", 18, (int?)null),
            (detail.Student!.MajorName, detail.Student.CampusName, detail.Student.EffectiveMaxCredits, detail.Student.MaxCreditsOverride));
    }

    [Fact]
    public async Task Create_Student_WithOverride_EffectiveMaxCreditsIsOverride()
    {
        var r = await StudentAsync();
        var detail = await CreateAsync(await AdminAsync(), r with { Student = r.Student! with { MaxCreditsOverride = 21 } });
        Assert.Equal((21, (int?)21), (detail.Student!.EffectiveMaxCredits, detail.Student.MaxCreditsOverride));
    }

    [Fact]
    public async Task Create_Instructor_Returns201WithDetail()
    {
        var detail = await CreateAsync(await AdminAsync(), await InstructorAsync());

        Assert.Equal(UserRole.Instructor, detail.Role);
        Assert.Null(detail.Student);
        Assert.Equal("Computer Science", detail.Instructor!.DepartmentName);
    }

    [Fact]
    public async Task Create_ResponseNeverContainsPassword()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, await StudentAsync(), Json);
        var body = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Password, body);
    }

    [Fact]
    public async Task Create_NewUserCanLogInWithInitialPassword()
    {
        var r = await StudentAsync();
        await CreateAsync(await AdminAsync(), r);

        var res = await LoginAsync(r.UniversityId, Password);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Student", (await res.Content.ReadFromJsonAsync<LoginResponse>())!.User.Role);
        await factory.WithDbAsync(async db =>
            Assert.NotEqual(Password, (await db.Set<User>().SingleAsync(u => u.UniversityId == r.UniversityId)).PasswordHash));
    }

    [Fact]
    public async Task Create_DuplicateUniversityId_Returns409()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, await StudentAsync(id: SisApiFactory.StudentId, email: EmailFor(NewId())), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_university_id", await CodeOf(res));
        Assert.Equal("University ID 3001 is already in use.", (await ProblemAsync(res)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Create_DuplicateEmailDifferentCase_Returns409()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, await StudentAsync(email: "Student@SmartSis.Local"), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_email", await CodeOf(res));
    }

    [Fact]
    public async Task Create_DuplicateOfDeactivatedUser_Returns409Not500()
    {
        var admin = await AdminAsync();
        var gone = await StudentAsync();
        var created = await CreateAsync(admin, gone);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Url}/{created.Id}/deactivate", null)).StatusCode);

        var sameId = await admin.PostAsJsonAsync(Url, await StudentAsync(id: gone.UniversityId, email: EmailFor(NewId())), Json);
        var sameEmail = await admin.PostAsJsonAsync(Url, await StudentAsync(email: gone.Email.ToUpperInvariant()), Json);

        Assert.Equal(HttpStatusCode.Conflict, sameId.StatusCode);
        Assert.Equal("duplicate_university_id", await CodeOf(sameId));
        Assert.Equal(HttpStatusCode.Conflict, sameEmail.StatusCode);
        Assert.Equal("duplicate_email", await CodeOf(sameEmail));
    }

    [Fact]
    public async Task Create_Admin_Returns400()
    {
        var r = await StudentAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r with { Role = UserRole.Admin, Student = null }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var errors = (await ProblemAsync(res)).GetProperty("errors").GetProperty("role");
        Assert.Contains(CreateUserRequestValidator.AdminNotAllowed, errors.EnumerateArray().Select(e => e.GetString()));
    }

    public static TheoryData<string, string> Mismatches => new()
    {
        { "student-without-block", "student" },
        { "student-with-instructor-block", "instructor" },
        { "instructor-without-block", "instructor" },
        { "instructor-with-student-block", "student" },
    };

    [Theory]
    [MemberData(nameof(Mismatches))]
    public async Task Create_RoleProfileMismatch_Returns400(string kind, string field)
    {
        var student = await StudentAsync();
        var instructor = await InstructorAsync();
        var r = kind switch
        {
            "student-without-block" => student with { Student = null },
            "student-with-instructor-block" => student with { Instructor = instructor.Instructor },
            "instructor-without-block" => instructor with { Instructor = null },
            "instructor-with-student-block" => instructor with { Student = student.Student },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True(await HasError(res, field), $"expected an error on '{field}'");
    }

    [Fact]
    public async Task Create_UnknownMajorAndCampus_Returns400OnStudentFields()
    {
        var r = await StudentAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r with { Student = r.Student! with { MajorId = 9999, CampusId = 8888 } }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True(await HasError(res, "student.majorId"));
        Assert.True(await HasError(res, "student.campusId"));
    }

    [Fact]
    public async Task Create_UnknownDepartment_Returns400OnInstructorField()
    {
        var r = await InstructorAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r with { Instructor = new InstructorInfoRequest(9999) }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True(await HasError(res, "instructor.departmentId"));
    }

    public static TheoryData<string, string> InvalidFields => new()
    {
        { "uid-zero", "universityId" },
        { "uid-3-digits", "universityId" },
        { "uid-10-digits", "universityId" },
        { "email-blank", "email" },
        { "email-invalid", "email" },
        { "email-too-long", "email" },
        { "name-blank", "fullName" },
        { "name-too-long", "fullName" },
        { "phone-too-long", "phone" },
        { "dob-future", "dateOfBirth" },
        { "password-short", "initialPassword" },
        { "password-no-digit", "initialPassword" },
        { "password-no-letter", "initialPassword" },
        { "override-zero", "student.maxCreditsOverride" },
        { "override-31", "student.maxCreditsOverride" },
        { "major-zero", "student.majorId" },
        { "enrollment-date-missing", "student.enrollmentDate" },
    };

    [Theory]
    [MemberData(nameof(InvalidFields))]
    public async Task Create_InvalidField_Returns400(string kind, string field)
    {
        var r = await StudentAsync();
        r = kind switch
        {
            "uid-zero" => r with { UniversityId = 0 },
            "uid-3-digits" => r with { UniversityId = 999 },
            "uid-10-digits" => r with { UniversityId = 1_000_000_000 },
            "email-blank" => r with { Email = " " },
            "email-invalid" => r with { Email = "not-an-email" },
            "email-too-long" => r with { Email = new string('a', 250) + "@x.test" },
            "name-blank" => r with { FullName = "" },
            "name-too-long" => r with { FullName = new string('a', 151) },
            "phone-too-long" => r with { Phone = new string('1', 33) },
            "dob-future" => r with { DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) },
            "password-short" => r with { InitialPassword = "Ab1" },
            "password-no-digit" => r with { InitialPassword = "abcdefghij" },
            "password-no-letter" => r with { InitialPassword = "1234567890" },
            "override-zero" => r with { Student = r.Student! with { MaxCreditsOverride = 0 } },
            "override-31" => r with { Student = r.Student! with { MaxCreditsOverride = 31 } },
            "major-zero" => r with { Student = r.Student! with { MajorId = 0 } },
            "enrollment-date-missing" => r with { Student = r.Student! with { EnrollmentDate = default } },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True(await HasError(res, field), $"expected an error on '{field}': {await res.Content.ReadAsStringAsync()}");
    }

    [Theory]
    [InlineData(1_000)]
    [InlineData(999_999_999)]
    public async Task Create_UniversityIdAtBounds_IsAllowed(long uid)
    {
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, await StudentAsync(id: uid, email: EmailFor(uid)), Json);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    // ───── update ─────

    [Fact]
    public async Task Update_Student_Persists()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();
        var created = await CreateAsync(admin, r);
        var newEmail = EmailFor(NewId());

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}", ToUpdate(r) with
        {
            Email = newEmail.ToUpperInvariant(),
            FullName = "Renamed Student",
            Phone = null,
            Student = r.Student! with { MaxCreditsOverride = 12, EnrollmentDate = new DateOnly(2024, 2, 1) },
        }, Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = (await admin.GetFromJsonAsync<UserDetailDto>($"{Url}/{created.Id}", Json))!;
        Assert.Equal((newEmail, "Renamed Student", (string?)null), (detail.Email, detail.FullName, detail.Phone));
        Assert.Equal((12, new DateOnly(2024, 2, 1)), (detail.Student!.EffectiveMaxCredits, detail.Student.EnrollmentDate));
        Assert.Equal(r.UniversityId, detail.UniversityId);
    }

    [Fact]
    public async Task Update_Instructor_ChangesDepartment()
    {
        var admin = await AdminAsync();
        var r = await InstructorAsync();
        var created = await CreateAsync(admin, r);
        int matDept = 0;
        await factory.WithDbAsync(async db => matDept = (await db.Set<Department>().SingleAsync(d => d.Code == "MAT")).Id);

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}", ToUpdate(r) with { Instructor = new InstructorInfoRequest(matDept) }, Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Mathematics", (await res.Content.ReadFromJsonAsync<UserDetailDto>(Json))!.Instructor!.DepartmentName);
    }

    [Fact]
    public async Task Update_KeepsOwnEmail_Returns200()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();
        var created = await CreateAsync(admin, r);

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}", ToUpdate(r) with { Email = r.Email.ToUpperInvariant() }, Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Update_EmailOfAnotherUser_Returns409()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();
        var created = await CreateAsync(admin, r);

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}", ToUpdate(r) with { Email = "instructor@smartsis.local" }, Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_email", await CodeOf(res));
    }

    [Fact]
    public async Task Update_WrongProfileForRole_Returns400()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();
        var created = await CreateAsync(admin, r);

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}", ToUpdate(r) with { Student = null, Instructor = new InstructorInfoRequest(1) }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True(await HasError(res, "student"));
        Assert.True(await HasError(res, "instructor"));
    }

    [Fact]
    public async Task Update_DeactivatedUser_Returns409()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();
        var created = await CreateAsync(admin, r);
        await admin.PostAsync($"{Url}/{created.Id}/deactivate", null);

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}", ToUpdate(r), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("user_inactive", await CodeOf(res));
    }

    [Fact]
    public async Task Update_Admin_Returns409()
    {
        var admin = await AdminAsync();
        int adminUserId = 0;
        await factory.WithDbAsync(async db => adminUserId = (await db.Set<User>().SingleAsync(u => u.UniversityId == SisApiFactory.AdminId)).Id);

        var res = await admin.PutAsJsonAsync($"{Url}/{adminUserId}",
            new UpdateUserRequest("admin@smartsis.local", "Demo Admin", null, null, null, null), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("admin_not_managed", await CodeOf(res));
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var res = await (await AdminAsync()).PutAsJsonAsync($"{Url}/999999", ToUpdate(await StudentAsync()), Json);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ───── deactivate ─────

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task Deactivate_UserCantLogInButStaysVisibleToAdmin()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();
        var created = await CreateAsync(admin, r);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(r.UniversityId, Password)).StatusCode);

        var res = await admin.PostAsync($"{Url}/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        // Login: same generic message as a wrong password (security.md).
        var login = await LoginAsync(r.UniversityId, Password);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(AuthService.InvalidCredentials, (await ProblemAsync(login)).GetProperty("title").GetString());

        // Still readable by id, marked inactive.
        var detail = (await admin.GetFromJsonAsync<UserDetailDto>($"{Url}/{created.Id}", Json))!;
        Assert.False(detail.IsActive);
        Assert.NotNull(detail.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, detail.DeletedAt!.Value.Kind);

        // In the Inactive and All lists, not in the default (Active) list.
        async Task<bool> Listed(string status) =>
            (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={r.UniversityId}{status}", Json))!
                .Items.Any(u => u.Id == created.Id);
        Assert.False(await Listed(""));
        Assert.False(await Listed("&status=Active"));
        Assert.True(await Listed("&status=Inactive"));
        Assert.True(await Listed("&status=All"));

        // The row is soft-deleted, not removed (D1).
        await factory.WithDbAsync(async db =>
        {
            var row = await db.Set<User>().IgnoreQueryFilters().SingleAsync(u => u.Id == created.Id);
            Assert.False(row.IsActive);
            Assert.NotNull(row.DeletedAt);
        });
    }

    [Fact]
    public async Task Deactivate_Twice_Returns204BothTimes()
    {
        var admin = await AdminAsync();
        var created = await CreateAsync(admin, await StudentAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Url}/{created.Id}/deactivate", null)).StatusCode);
        var first = (await admin.GetFromJsonAsync<UserDetailDto>($"{Url}/{created.Id}", Json))!.DeletedAt;
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Url}/{created.Id}/deactivate", null)).StatusCode);
        Assert.Equal(first, (await admin.GetFromJsonAsync<UserDetailDto>($"{Url}/{created.Id}", Json))!.DeletedAt);
    }

    [Fact]
    public async Task Deactivate_Self_Returns409()
    {
        var admin = await AdminAsync();
        int adminUserId = 0;
        await factory.WithDbAsync(async db => adminUserId = (await db.Set<User>().SingleAsync(u => u.UniversityId == SisApiFactory.AdminId)).Id);

        var res = await admin.PostAsync($"{Url}/{adminUserId}/deactivate", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("cannot_deactivate_self", await CodeOf(res));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(SisApiFactory.AdminId, "Pass@1234")).StatusCode);
    }

    [Fact]
    public async Task Deactivate_OtherAdmin_Returns409()
    {
        var admin = await AdminAsync();
        var otherAdminUid = NewId();
        int otherAdminId = 0;
        await factory.WithDbAsync(async db =>
        {
            var u = new User { UniversityId = otherAdminUid, Email = EmailFor(otherAdminUid), FullName = "Simulated Admin", Role = UserRole.Admin, PasswordHash = "x" };
            db.Add(u);
            await db.SaveChangesAsync();
            otherAdminId = u.Id;
        });

        var res = await admin.PostAsync($"{Url}/{otherAdminId}/deactivate", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("admin_not_managed", await CodeOf(res));
    }

    [Fact]
    public async Task Deactivate_UnknownId_Returns404()
    {
        var res = await (await AdminAsync()).PostAsync($"{Url}/999999/deactivate", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ───── list / get ─────

    [Fact]
    public async Task List_FiltersAndPagesOrderedByName()
    {
        var admin = await AdminAsync();
        var tag = "Zed" + Guid.NewGuid().ToString("N")[..6];
        var created = new List<UserDetailDto>();
        foreach (var n in new[] { "Charlie", "Alpha", "Bravo" })
        {
            var r = await StudentAsync();
            created.Add(await CreateAsync(admin, r with { FullName = $"{tag} {n}" }));
        }
        var instructor = await InstructorAsync();
        await CreateAsync(admin, instructor with { FullName = $"{tag} Delta" });

        var page1 = (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={tag.ToUpperInvariant()}&role=Student&pageSize=2", Json))!;
        var page2 = (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={tag}&role=Student&page=2&pageSize=2", Json))!;

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal([$"{tag} Alpha", $"{tag} Bravo"], page1.Items.Select(u => u.FullName));
        Assert.Equal([$"{tag} Charlie"], page2.Items.Select(u => u.FullName));
        Assert.All(page1.Items, u => Assert.Equal(("Computer Science", (string?)null), (u.MajorName, u.DepartmentName)));

        var l = await LookupsAsync();
        var byDept = (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={tag}&departmentId={l.CscDept}", Json))!;
        Assert.Equal(4, byDept.TotalCount); // 3 students via their major + 1 instructor
        var byMajor = (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={tag}&majorId={l.Major}", Json))!;
        Assert.Equal(3, byMajor.TotalCount);
        var instructors = (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={tag}&role=Instructor", Json))!;
        Assert.Equal("Computer Science", Assert.Single(instructors.Items).DepartmentName);
    }

    [Fact]
    public async Task List_SearchMatchesEmailAndUniversityId()
    {
        var admin = await AdminAsync();
        var r = await StudentAsync();
        var created = await CreateAsync(admin, r);

        var byEmail = (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={Uri.EscapeDataString(r.Email.ToUpperInvariant())}", Json))!;
        var byId = (await admin.GetFromJsonAsync<PagedResult<UserListItemDto>>($"{Url}?search={r.UniversityId}", Json))!;

        Assert.Equal(created.Id, Assert.Single(byEmail.Items).Id);
        Assert.Equal(created.Id, Assert.Single(byId.Items).Id);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var res = await (await AdminAsync()).GetAsync($"{Url}/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
