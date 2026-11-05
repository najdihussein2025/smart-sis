using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Enums;
using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Sections;

/// <summary>
/// Admin sections API (PB-02, UC-16, SD-16). Clash tests run against the seeded current term (the seeded instructor
/// teaches every seeded section). Tests that save sections use their own new term, so they can't clash with each other
/// whatever order xUnit runs them in.
/// </summary>
[Trait("SR", "SR-21")]
public sealed class SectionsApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/sections";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private static int _next;

    /// <summary>"T1", "T2", … — unique per test in this class.</summary>
    private static string UniqueCode() => $"T{Interlocked.Increment(ref _next)}";

    private sealed record Seed(int TermId, int InstructorId, Dictionary<string, int> Courses, Dictionary<string, int> Rooms, int StudentId);

    private async Task<Seed> SeedAsync()
    {
        Seed? seed = null;
        await factory.WithDbAsync(async db =>
        {
            var term = await db.Set<Term>().SingleAsync(t => t.IsCurrent);
            var instructor = await db.Set<Instructor>().SingleAsync(i => i.User.UniversityId == SisApiFactory.InstructorId);
            var student = await db.Set<Student>().SingleAsync(s => s.User.UniversityId == SisApiFactory.StudentId);
            seed = new Seed(
                term.Id,
                instructor.UserId,
                await db.Set<Course>().ToDictionaryAsync(c => c.Code, c => c.Id),
                await db.Set<Room>().ToDictionaryAsync(r => r.Name, r => r.Id),
                student.UserId);
        });
        return seed!;
    }

    private Task<HttpClient> AdminAsync() => factory.ClientForAsync(SisApiFactory.AdminId);

    /// <summary>An empty term only this test uses.</summary>
    private async Task<int> NewTermAsync()
    {
        var id = 0;
        await factory.WithDbAsync(async db =>
        {
            static DateTime Utc(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);
            var t = new Term
            {
                Name = $"Sections test term {UniqueCode()}", StartDate = new DateOnly(2030, 1, 1), EndDate = new DateOnly(2030, 5, 1),
                EnrollStart = Utc(2029, 12, 1), EnrollEnd = Utc(2029, 12, 10), AddDropStart = Utc(2029, 12, 10), AddDropEnd = Utc(2030, 1, 10),
            };
            db.Add(t);
            await db.SaveChangesAsync();
            id = t.Id;
        });
        return id;
    }

    /// <summary>The seed, with <see cref="Seed.TermId"/> pointing at a fresh term.</summary>
    private async Task<Seed> IsolatedSeedAsync() => await SeedAsync() with { TermId = await NewTermAsync() };

    private static SaveMeetingRequest Meeting(int day, int hour, int minutes, int roomId, int? id = null, MeetingType type = MeetingType.Lecture) =>
        new(id, day, new TimeOnly(hour, 0), new TimeOnly(hour, 0).AddMinutes(minutes), roomId, type);

    private static SaveSectionRequest Request(Seed s, string code, int? instructorId, params SaveMeetingRequest[] meetings) =>
        new(s.Courses["CSC 101"], s.TermId, code, instructorId, 30, meetings);

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage res) =>
        JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

    private static async Task<string?> CodeOf(HttpResponseMessage res) =>
        (await ProblemAsync(res)).TryGetProperty("code", out var c) ? c.GetString() : null;

    private async Task<SectionDto> CreateAsync(HttpClient admin, SaveSectionRequest request)
    {
        var res = await admin.PostAsJsonAsync(Url, request, Json);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<SectionDto>(Json))!;
    }

    [Fact]
    public async Task Create_ValidSection_PersistsWithMeetings()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var code = UniqueCode();

        var res = await admin.PostAsJsonAsync(Url, Request(s, code.ToLower(), s.InstructorId,
            Meeting(6, 8, 75, s.Rooms["B-101"]), Meeting(6, 10, 120, s.Rooms["Lab-3"], type: MeetingType.Lab)), Json);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = (await res.Content.ReadFromJsonAsync<SectionDto>(Json))!;
        Assert.Equal($"{Url}/{created.Id}", res.Headers.Location!.AbsolutePath, ignoreCase: true);

        var detail = (await admin.GetFromJsonAsync<SectionDto>(res.Headers.Location, Json))!;
        Assert.Equal(code, detail.SectionCode); // stored upper case
        Assert.Equal("CSC 101", detail.CourseCode);
        Assert.Equal("Demo Instructor", detail.InstructorName);
        Assert.Equal(0, detail.SeatsTaken);
        Assert.Equal(["B-101", "Lab-3"], detail.Meetings.Select(m => m.RoomName));
        Assert.Equal(MeetingType.Lab, detail.Meetings[1].Type);
        Assert.Equal(new TimeOnly(9, 15), detail.Meetings[0].EndTime);
    }

    [Fact]
    public async Task Create_TimesAndTypeAsStrings_Accepted()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var body = $$"""
            { "courseId": {{s.Courses["CSC 210"]}}, "termId": {{s.TermId}}, "sectionCode": "{{UniqueCode()}}", "instructorId": null,
              "capacity": 25, "meetings": [ { "dayOfWeek": 6, "startTime": "16:00:00", "endTime": "17:15:00", "roomId": {{s.Rooms["B-204"]}}, "type": "Lab" } ] }
            """;

        var res = await admin.PostAsync(Url, new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var text = await res.Content.ReadAsStringAsync();
        Assert.Contains("\"type\":\"Lab\"", text);
        Assert.Contains("\"startTime\":\"16:00:00\"", text);
    }

    [Fact]
    public async Task Create_NoInstructor_Allowed()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();

        var created = await CreateAsync(admin, Request(s, UniqueCode(), null, Meeting(6, 18, 60, s.Rooms["B-101"])));

        Assert.Null(created.InstructorId);
        Assert.Null(created.InstructorName);
    }

    [Fact]
    public async Task Create_SeededRoomBusy_Returns409RoomClash()
    {
        var s = await SeedAsync();
        // Seeded CSC 101 A uses B-101 on Monday 08:00–09:15.
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(s, UniqueCode(), null, Meeting(1, 8, 60, s.Rooms["B-101"])), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("room_clash", await CodeOf(res));
        Assert.Equal("Room B-101 is already used by CSC 101 A on Monday 08:00–09:15.", (await ProblemAsync(res)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Create_InstructorBusy_Returns409InstructorClash()
    {
        var s = await SeedAsync();
        // Lab-3 is free on Monday morning, but the seeded instructor teaches CSC 101 A then.
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(s, UniqueCode(), s.InstructorId, Meeting(1, 8, 60, s.Rooms["Lab-3"])), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("instructor_clash", await CodeOf(res));
    }

    [Fact]
    public async Task Create_OwnMeetingsOverlap_Returns409()
    {
        var s = await SeedAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(s, UniqueCode(), null,
            Meeting(6, 12, 90, s.Rooms["B-101"]), Meeting(6, 13, 60, s.Rooms["B-204"])), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("meeting_overlap", await CodeOf(res));
    }

    [Fact]
    public async Task Create_ClashInOtherTerm_Allowed()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();

        // Same slot, room and instructor as seeded CSC 101 A, but a different term.
        await CreateAsync(admin, Request(s, UniqueCode(), s.InstructorId, Meeting(1, 8, 75, s.Rooms["B-101"])));
    }

    [Fact]
    public async Task Create_DuplicateCodeForCourseAndTerm_Returns409()
    {
        var s = await SeedAsync();
        // Seeded: CSC 101 section A in the current term.
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(s, "a", null, Meeting(6, 19, 60, s.Rooms["B-204"])), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_section_code", await CodeOf(res));
    }

    [Fact]
    public async Task Create_UnknownReferences_Returns400OnFields()
    {
        var s = await SeedAsync();
        var r = new SaveSectionRequest(99999, 88888, UniqueCode(), 77777, 30,
            [Meeting(6, 8, 60, s.Rooms["B-101"]), Meeting(6, 9, 60, 66666)]);

        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var errors = (await ProblemAsync(res)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("courseId", out _));
        Assert.True(errors.TryGetProperty("termId", out _));
        Assert.True(errors.TryGetProperty("instructorId", out _));
        Assert.True(errors.TryGetProperty("meetings[1].roomId", out _));
        Assert.False(errors.TryGetProperty("meetings[0].roomId", out _));
    }

    [Fact]
    public async Task Create_StudentAsInstructor_Returns400()
    {
        var s = await SeedAsync();
        var res = await (await AdminAsync()).PostAsJsonAsync(Url, Request(s, UniqueCode(), s.StudentId, Meeting(6, 8, 60, s.Rooms["B-204"])), Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True((await ProblemAsync(res)).GetProperty("errors").TryGetProperty("instructorId", out _));
    }

    public static TheoryData<string, string> InvalidRequests => new()
    {
        { "blank-code", "sectionCode" },
        { "long-code", "sectionCode" },
        { "symbol-code", "sectionCode" },
        { "capacity-0", "capacity" },
        { "capacity-501", "capacity" },
        { "no-meetings", "meetings" },
        { "day-0", "meetings[0].dayOfWeek" },
        { "day-7", "meetings[0].dayOfWeek" },
        { "end-before-start", "meetings[0].endTime" },
        { "bad-type", "meetings[0].type" },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_InvalidField_Returns400(string kind, string field)
    {
        var s = await SeedAsync();
        var r = Request(s, UniqueCode(), null, Meeting(6, 8, 60, s.Rooms["B-101"]));
        var m = r.Meetings[0];
        r = kind switch
        {
            "blank-code" => r with { SectionCode = " " },
            "long-code" => r with { SectionCode = "ABCDEFGHI" },
            "symbol-code" => r with { SectionCode = "A-1" },
            "capacity-0" => r with { Capacity = 0 },
            "capacity-501" => r with { Capacity = 501 },
            "no-meetings" => r with { Meetings = [] },
            "day-0" => r with { Meetings = [m with { DayOfWeek = 0 }] },
            "day-7" => r with { Meetings = [m with { DayOfWeek = 7 }] },
            "end-before-start" => r with { Meetings = [m with { EndTime = m.StartTime.AddMinutes(-30) }] },
            "bad-type" => r with { Meetings = [m with { Type = (MeetingType)9 }] },
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        var res = await (await AdminAsync()).PostAsJsonAsync(Url, r, Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True((await ProblemAsync(res)).GetProperty("errors").TryGetProperty(field, out _), $"expected an error on {field}");
    }

    [Fact]
    public async Task Update_KeepsMeetingIdsAndReplacesTheRest()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var created = await CreateAsync(admin, Request(s, UniqueCode(), null,
            Meeting(6, 7, 60, s.Rooms["B-101"]), Meeting(6, 8, 60, s.Rooms["B-101"])));
        var kept = created.Meetings[0];

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}", Request(s, created.SectionCode, s.InstructorId,
            Meeting(6, 7, 90, s.Rooms["B-204"], id: kept.Id), Meeting(6, 9, 60, s.Rooms["Lab-3"])) with { Capacity = 12 }, Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = (await res.Content.ReadFromJsonAsync<SectionDto>(Json))!;
        Assert.Equal(12, updated.Capacity);
        Assert.Equal(s.InstructorId, updated.InstructorId);
        Assert.Equal(2, updated.Meetings.Count);
        Assert.Equal(kept.Id, updated.Meetings[0].Id);
        Assert.Equal("B-204", updated.Meetings[0].RoomName);
        Assert.DoesNotContain(updated.Meetings, x => x.Id == created.Meetings[1].Id);
    }

    [Fact]
    public async Task Update_OwnMeetingsDoNotClashWithThemselves()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var created = await CreateAsync(admin, Request(s, UniqueCode(), null, Meeting(6, 11, 60, s.Rooms["Lab-3"])));

        // Same slot again: must not report a room clash with its own stored meeting.
        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}",
            Request(s, created.SectionCode, null, Meeting(6, 11, 60, s.Rooms["Lab-3"], id: created.Meetings[0].Id)), Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Update_MeetingIdFromAnotherSection_Returns400()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var a = await CreateAsync(admin, Request(s, UniqueCode(), null, Meeting(6, 13, 30, s.Rooms["Lab-3"])));
        var b = await CreateAsync(admin, Request(s, UniqueCode(), null, Meeting(6, 14, 30, s.Rooms["Lab-3"])));

        var res = await admin.PutAsJsonAsync($"{Url}/{a.Id}",
            Request(s, a.SectionCode, null, Meeting(6, 13, 30, s.Rooms["Lab-3"], id: b.Meetings[0].Id)), Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.True((await ProblemAsync(res)).GetProperty("errors").TryGetProperty("meetings[0].id", out _));
    }

    [Fact]
    public async Task Update_CapacityBelowSeatsTaken_Returns409()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var created = await CreateAsync(admin, Request(s, UniqueCode(), null, Meeting(6, 15, 30, s.Rooms["Lab-3"])) with { Capacity = 5 });
        await AddEnrollmentsAsync(created.Id, s.StudentId, EnrollmentStatus.Enrolled, EnrollmentStatus.Completed, EnrollmentStatus.Dropped);

        var detail = (await admin.GetFromJsonAsync<SectionDto>($"{Url}/{created.Id}", Json))!;
        Assert.Equal(2, detail.SeatsTaken); // the dropped one doesn't hold a seat

        var tooLow = await admin.PutAsJsonAsync($"{Url}/{created.Id}",
            Request(s, created.SectionCode, null, Meeting(6, 15, 30, s.Rooms["Lab-3"], id: created.Meetings[0].Id)) with { Capacity = 1 }, Json);
        Assert.Equal(HttpStatusCode.Conflict, tooLow.StatusCode);
        Assert.Equal("capacity_below_taken", await CodeOf(tooLow));

        var exact = await admin.PutAsJsonAsync($"{Url}/{created.Id}",
            Request(s, created.SectionCode, null, Meeting(6, 15, 30, s.Rooms["Lab-3"], id: created.Meetings[0].Id)) with { Capacity = 2 }, Json);
        Assert.Equal(HttpStatusCode.OK, exact.StatusCode);
    }

    [Fact]
    public async Task Update_ChangeCourseWithEnrollments_Returns409()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var created = await CreateAsync(admin, Request(s, UniqueCode(), null, Meeting(6, 16, 30, s.Rooms["Lab-3"])));
        await AddEnrollmentsAsync(created.Id, s.StudentId, EnrollmentStatus.Enrolled);

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}",
            Request(s, created.SectionCode, null, Meeting(6, 16, 30, s.Rooms["Lab-3"], id: created.Meetings[0].Id)) with { CourseId = s.Courses["MAT 201"] }, Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("section_has_enrollments", await CodeOf(res));
    }

    [Fact]
    public async Task Update_RemoveMeetingWithAttendance_Returns409()
    {
        var admin = await AdminAsync();
        var s = await IsolatedSeedAsync();
        var created = await CreateAsync(admin, Request(s, UniqueCode(), null,
            Meeting(6, 17, 30, s.Rooms["Lab-3"]), Meeting(6, 18, 30, s.Rooms["Lab-3"])));
        var used = created.Meetings[0];
        await factory.WithDbAsync(async db =>
        {
            db.Add(new AttendanceSession
            {
                SectionId = created.Id, MeetingId = used.Id, SessionDate = new DateOnly(2026, 11, 7),
                SubmittedBy = s.InstructorId, SubmittedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        var res = await admin.PutAsJsonAsync($"{Url}/{created.Id}",
            Request(s, created.SectionCode, null, Meeting(6, 18, 30, s.Rooms["Lab-3"], id: created.Meetings[1].Id)), Json);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("meeting_has_attendance", await CodeOf(res));
    }

    [Fact]
    public async Task Update_UnknownSection_Returns404()
    {
        var s = await SeedAsync();
        var res = await (await AdminAsync()).PutAsJsonAsync($"{Url}/99999", Request(s, UniqueCode(), null, Meeting(6, 8, 30, s.Rooms["B-101"])), Json);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByCourseAndTerm()
    {
        var admin = await AdminAsync();
        var s = await SeedAsync();

        var page = (await admin.GetFromJsonAsync<PagedResult<SectionDto>>($"{Url}?termId={s.TermId}&courseId={s.Courses["CSC 210"]}", Json))!;

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, x => Assert.Equal("CSC 210", x.CourseCode));
        Assert.Contains(page.Items, x => x.SectionCode == "A");
        Assert.Contains(page.Items, x => x.SectionCode == "B");
    }

    [Fact]
    public async Task List_SearchMatchesCourseName()
    {
        var admin = await AdminAsync();
        var page = (await admin.GetFromJsonAsync<PagedResult<SectionDto>>($"{Url}?search=discrete", Json))!;

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, x => Assert.Equal("MAT 201", x.CourseCode));
    }

    [Fact]
    public async Task Get_Unknown_Returns404()
    {
        var res = await (await AdminAsync()).GetAsync($"{Url}/99999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private Task AddEnrollmentsAsync(int sectionId, int studentId, params EnrollmentStatus[] statuses) =>
        factory.WithDbAsync(async db =>
        {
            foreach (var status in statuses)
                db.Add(new Enrollment { SectionId = sectionId, StudentId = studentId, Status = status, EnrolledAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        });
}
