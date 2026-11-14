using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Announcements.Interfaces;
using SmartSis.Api.Features.Announcements.Models.Dtos;
using SmartSis.Api.Features.Announcements.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Announcements;

/// <summary>
/// PB-12 / T46: feed per role, admin and instructor posts (rule C1), scheduling and notifications.
/// Runs against the seeded demo announcements (AnnouncementsDevSeed). Tests in a class run one at a time.
/// </summary>
public sealed class AnnouncementsApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const long OtherStudent = 3004; // enrolled in CSC 101 A and MAT 201 A, not CSC 210 A

    private static SaveAnnouncementRequest AdminPost(
        string title, AnnouncementScope scope = AnnouncementScope.All, int? majorId = null, DateTime? publishAt = null, DateTime? expiresAt = null) =>
        new(title, "Simulated announcement body.", scope, majorId, null, publishAt, expiresAt);

    private async Task<List<AnnouncementDto>> FeedAsync(long universityId)
    {
        var client = await factory.ClientForAsync(universityId);
        return (await (await client.GetAsync("/api/announcements?pageSize=100")).ReadAsync<PagedResult<AnnouncementDto>>()).Items.ToList();
    }

    private async Task<AnnouncementDto> CreateAsAdminAsync(SaveAnnouncementRequest request)
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        var res = await admin.PostAsJsonAsync("/api/admin/announcements", request, TestData.Json);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await res.ReadAsync<AnnouncementDto>();
    }

    private async Task<List<Notification>> NotificationsOfAsync(int announcementId)
    {
        var list = new List<Notification>();
        await factory.WithDbAsync(async db => list = await db.Set<Notification>()
            .Where(n => n.Link != null && n.Link.EndsWith($"?announcement={announcementId}")).ToListAsync());
        return list;
    }

    [Fact]
    public async Task Feed_Student_SeesAllStudentsOwnMajorAndOwnSections()
    {
        var titles = (await FeedAsync(SisApiFactory.StudentId)).Select(a => a.Title).ToList();

        Assert.Contains("Welcome to the new Smart SIS", titles);          // All
        Assert.Contains("Add/drop week is coming", titles);               // Students
        Assert.Contains("Computer Science advising hours", titles);       // own major
        Assert.Contains("CSC 210 A: lab moved this week", titles);        // enrolled section
        Assert.DoesNotContain("Attendance must be submitted weekly", titles); // Instructors
        Assert.DoesNotContain("Library closed for inventory", titles);    // expired
        Assert.DoesNotContain("Midterm exam schedule", titles);           // scheduled
    }

    [Fact]
    public async Task Feed_StudentNotInSection_DoesNotSeeSectionPost()
    {
        var titles = (await FeedAsync(OtherStudent)).Select(a => a.Title).ToList();
        Assert.Contains("Welcome to the new Smart SIS", titles);
        Assert.DoesNotContain("CSC 210 A: lab moved this week", titles);
    }

    [Fact]
    public async Task Feed_Instructor_SeesAllInstructorsAndOwnSections_NotStudentsOnly()
    {
        var feed = await FeedAsync(SisApiFactory.InstructorId);
        var titles = feed.Select(a => a.Title).ToList();

        Assert.Contains("Welcome to the new Smart SIS", titles);
        Assert.Contains("Attendance must be submitted weekly", titles);
        Assert.Contains("CSC 210 A: lab moved this week", titles);
        Assert.DoesNotContain("Add/drop week is coming", titles);
        Assert.DoesNotContain("Computer Science advising hours", titles);
        Assert.True(feed.Single(a => a.Title == "CSC 210 A: lab moved this week").CanManage);
        Assert.False(feed.Single(a => a.Title == "Welcome to the new Smart SIS").CanManage);
    }

    [Fact]
    public async Task Feed_IsNewestFirst()
    {
        var feed = await FeedAsync(SisApiFactory.StudentId);
        Assert.Equal(feed.OrderByDescending(a => a.PublishAt).Select(a => a.Id), feed.Select(a => a.Id));
        Assert.All(feed, a => Assert.Equal(AnnouncementStatus.Live, a.Status));
    }

    [Fact]
    public async Task Feed_Admin_SeesEveryLivePostAndOwnScheduled()
    {
        var feed = await FeedAsync(SisApiFactory.AdminId);
        var titles = feed.Select(a => a.Title).ToList();

        Assert.Contains("Add/drop week is coming", titles);
        Assert.Contains("Attendance must be submitted weekly", titles);
        Assert.Contains("CSC 210 A: lab moved this week", titles);
        Assert.DoesNotContain("Library closed for inventory", titles);
        Assert.Equal(AnnouncementStatus.Scheduled, feed.Single(a => a.Title == "Midterm exam schedule").Status);
        Assert.All(feed, a => Assert.True(a.CanManage));
    }

    [Fact]
    public async Task AdminPost_Live_NotifiesEveryoneButTheAuthor()
    {
        var created = await CreateAsAdminAsync(AdminPost("Campus closed on Monday (test)"));

        Assert.Equal(AnnouncementStatus.Live, created.Status);
        Assert.Equal("Demo Admin", created.AuthorName);
        var notified = await NotificationsOfAsync(created.Id);
        var admin = await factory.UserIdAsync(SisApiFactory.AdminId);
        var student = await factory.UserIdAsync(SisApiFactory.StudentId);
        var instructor = await factory.UserIdAsync(SisApiFactory.InstructorId);
        Assert.DoesNotContain(notified, n => n.UserId == admin);
        Assert.Equal($"/student/announcements?announcement={created.Id}", notified.Single(n => n.UserId == student).Link);
        Assert.Equal($"/instructor/announcements?announcement={created.Id}", notified.Single(n => n.UserId == instructor).Link);
        Assert.All(notified, n => Assert.Equal(NotificationType.Announcement, n.Type));
        Assert.Contains("Campus closed on Monday (test)", (await FeedAsync(OtherStudent)).Select(a => a.Title));
    }

    [Fact]
    public async Task AdminPost_DoesNotNotifyDeactivatedUsers()
    {
        await factory.AddStudentAsync(3901);
        var deactivated = await factory.UserIdAsync(3901);
        await factory.WithDbAsync(async db =>
        {
            var u = await db.Set<SmartSis.Api.Features.Users.Models.Entities.User>().SingleAsync(x => x.Id == deactivated);
            u.MarkDeleted(DateTime.UtcNow);
            await db.SaveChangesAsync();
        });

        var created = await CreateAsAdminAsync(AdminPost("Only active users (test)", AnnouncementScope.Students));

        var notified = (await NotificationsOfAsync(created.Id)).Select(n => n.UserId).ToList();
        Assert.NotEmpty(notified);
        Assert.DoesNotContain(deactivated, notified);
    }

    [Fact]
    public async Task AdminPost_Major_NotifiesOnlyStudentsOfThatMajor()
    {
        var mathMajor = 0;
        await factory.WithDbAsync(async db => mathMajor = await db.Set<Major>().Where(m => m.Name != "Computer Science").Select(m => m.Id).FirstOrDefaultAsync());
        if (mathMajor == 0)
            await factory.WithDbAsync(async db =>
            {
                var m = new Major { DepartmentId = (await db.Set<Major>().FirstAsync()).DepartmentId, Name = "Applied Mathematics (test)", DefaultMaxCredits = 18 };
                db.Add(m);
                await db.SaveChangesAsync();
                mathMajor = m.Id;
            });

        var created = await CreateAsAdminAsync(AdminPost("Math majors only (test)", AnnouncementScope.Major, mathMajor));

        Assert.Empty(await NotificationsOfAsync(created.Id)); // every seeded student is Computer Science
        Assert.DoesNotContain("Math majors only (test)", (await FeedAsync(SisApiFactory.StudentId)).Select(a => a.Title));
    }

    [Fact]
    public async Task AdminPost_Scheduled_HiddenAndSilentUntilPublishTime_ThenNotifiedOnce()
    {
        var created = await CreateAsAdminAsync(AdminPost("Exam rooms announced (test)", publishAt: DateTime.UtcNow.AddDays(2)));

        Assert.Equal(AnnouncementStatus.Scheduled, created.Status);
        Assert.Empty(await NotificationsOfAsync(created.Id));
        Assert.DoesNotContain("Exam rooms announced (test)", (await FeedAsync(SisApiFactory.StudentId)).Select(a => a.Title));

        // The publish time comes: the job's run notifies the audience, and a second run doesn't again.
        await factory.WithDbAsync(async db =>
        {
            var a = await db.Set<Announcement>().SingleAsync(x => x.Id == created.Id);
            a.PublishAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });
        async Task RunJobAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IAnnouncementService>().NotifyDueAsync(TimeSpan.FromDays(7), default);
        }

        await RunJobAsync();
        var notified = (await NotificationsOfAsync(created.Id)).Count;
        Assert.True(notified > 0);
        await RunJobAsync();
        Assert.Equal(notified, (await NotificationsOfAsync(created.Id)).Count);
        Assert.Contains("Exam rooms announced (test)", (await FeedAsync(SisApiFactory.StudentId)).Select(a => a.Title));
    }

    [Fact]
    public async Task AdminPost_MajorWithoutMajorId_Returns400()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        var res = await admin.PostAsJsonAsync("/api/admin/announcements", AdminPost("No major", AnnouncementScope.Major), TestData.Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("majorId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task AdminPost_UnknownMajor_Returns400()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        var res = await admin.PostAsJsonAsync("/api/admin/announcements", AdminPost("Ghost major", AnnouncementScope.Major, 9999), TestData.Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("majorId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task AdminPost_SectionScope_Returns400()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        var sectionId = await factory.SectionIdAsync("CSC 101");
        var res = await admin.PostAsJsonAsync("/api/admin/announcements",
            new SaveAnnouncementRequest("Section post", "Body", AnnouncementScope.Section, null, sectionId, null, null), TestData.Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("scope", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task AdminPost_EmptyTitleAndPastExpiry_Returns400()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);

        var empty = await admin.PostAsJsonAsync("/api/admin/announcements", AdminPost(" "), TestData.Json);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Contains("title", (await empty.ReadAsync<ValidationProblemDetails>()).Errors.Keys);

        var expired = await admin.PostAsJsonAsync("/api/admin/announcements", AdminPost("Old news", expiresAt: DateTime.UtcNow.AddHours(-1)), TestData.Json);
        Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
        Assert.Equal(["The expiry must be in the future."], (await expired.ReadAsync<ValidationProblemDetails>()).Errors["expiresAt"]);
    }

    [Fact]
    public async Task AdminList_IncludesScheduledAndExpired_AndFiltersByStatus()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        var all = (await (await admin.GetAsync("/api/admin/announcements?pageSize=100")).ReadAsync<PagedResult<AnnouncementDto>>()).Items;
        Assert.Contains(all, a => a.Title == "Library closed for inventory" && a.Status == AnnouncementStatus.Expired);
        Assert.Contains(all, a => a.Title == "Midterm exam schedule" && a.Status == AnnouncementStatus.Scheduled);

        var expired = (await (await admin.GetAsync("/api/admin/announcements?status=Expired")).ReadAsync<PagedResult<AnnouncementDto>>()).Items;
        Assert.All(expired, a => Assert.Equal(AnnouncementStatus.Expired, a.Status));
        Assert.Contains(expired, a => a.Title == "Library closed for inventory");
    }

    [Fact]
    public async Task AdminUpdate_ChangesTextAndKeepsPublishDate()
    {
        var created = await CreateAsAdminAsync(AdminPost("Typo in titel (test)", AnnouncementScope.Students));
        var notifiedBefore = (await NotificationsOfAsync(created.Id)).Count;
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);

        var res = await admin.PutAsJsonAsync($"/api/admin/announcements/{created.Id}",
            AdminPost("Typo in title fixed (test)", AnnouncementScope.Students), TestData.Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var updated = await res.ReadAsync<AnnouncementDto>();
        Assert.Equal("Typo in title fixed (test)", updated.Title);
        Assert.Equal(created.PublishAt, updated.PublishAt);
        Assert.True(notifiedBefore > 0);
        Assert.Equal(notifiedBefore, (await NotificationsOfAsync(created.Id)).Count); // an edit doesn't notify again
    }

    [Fact]
    public async Task AdminDelete_RemovesFromFeed()
    {
        var created = await CreateAsAdminAsync(AdminPost("Delete me (test)"));
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/announcements/{created.Id}")).StatusCode);
        Assert.DoesNotContain("Delete me (test)", (await FeedAsync(SisApiFactory.StudentId)).Select(a => a.Title));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/admin/announcements/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task InstructorSections_ListsCurrentSectionsTaught()
    {
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var sections = await (await instructor.GetAsync("/api/instructor/announcements/sections")).ReadAsync<List<AnnouncementSectionDto>>();

        Assert.Equal(["CSC 101 A", "CSC 210 A", "CSC 210 B", "CSC 310 A", "MAT 201 A"], sections.Select(s => s.Label));
        Assert.True(sections.Single(s => s.Label == "CSC 210 A").EnrolledCount >= 1);
    }

    [Fact]
    public async Task InstructorPost_OwnSection_NotifiesEnrolledStudentsOnly()
    {
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var sectionId = await factory.SectionIdAsync("CSC 210", "A");

        var res = await instructor.PostAsJsonAsync("/api/instructor/announcements",
            new SaveSectionAnnouncementRequest("Quiz on Thursday (test)", "Chapters 3 and 4.", sectionId, null, null), TestData.Json);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.ReadAsync<AnnouncementDto>();
        Assert.Equal(AnnouncementScope.Section, created.Scope);
        Assert.Equal("CSC 210 A", created.SectionLabel);

        var notified = (await NotificationsOfAsync(created.Id)).Select(n => n.UserId).ToHashSet();
        Assert.Contains(await factory.UserIdAsync(SisApiFactory.StudentId), notified);
        Assert.DoesNotContain(await factory.UserIdAsync(OtherStudent), notified);
        Assert.DoesNotContain(await factory.UserIdAsync(SisApiFactory.InstructorId), notified);
        Assert.Contains("Quiz on Thursday (test)", (await FeedAsync(SisApiFactory.StudentId)).Select(a => a.Title));
        Assert.DoesNotContain("Quiz on Thursday (test)", (await FeedAsync(OtherStudent)).Select(a => a.Title));
    }

    [Fact]
    public async Task InstructorPost_SectionNotTaught_Returns403()
    {
        var sectionId = 0;
        await factory.WithDbAsync(async db =>
        {
            var model = await db.Set<Section>().Where(s => s.Term.IsCurrent).FirstAsync();
            var term = await db.Set<Term>().SingleAsync(t => t.IsCurrent);
            var s = new Section { CourseId = model.CourseId, TermId = term.Id, SectionCode = "Z9", Capacity = 10 };
            db.Add(s);
            await db.SaveChangesAsync();
            sectionId = s.Id;
        });
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);

        var res = await instructor.PostAsJsonAsync("/api/instructor/announcements",
            new SaveSectionAnnouncementRequest("Not mine", "Body", sectionId, null, null), TestData.Json);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task InstructorPost_UnknownSection_Returns400()
    {
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var res = await instructor.PostAsJsonAsync("/api/instructor/announcements",
            new SaveSectionAnnouncementRequest("Nowhere", "Body", 99999, null, null), TestData.Json);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("sectionId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task Instructor_CannotEditOrDeleteAdminPost_ButCanEditOwn()
    {
        var adminPost = await CreateAsAdminAsync(AdminPost("Admin only edits this (test)"));
        var instructor = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var sectionId = await factory.SectionIdAsync("CSC 101");

        var edit = await instructor.PutAsJsonAsync($"/api/instructor/announcements/{adminPost.Id}",
            new SaveSectionAnnouncementRequest("Hijack", "Body", sectionId, null, null), TestData.Json);
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await instructor.DeleteAsync($"/api/instructor/announcements/{adminPost.Id}")).StatusCode);

        var own = await (await instructor.PostAsJsonAsync("/api/instructor/announcements",
            new SaveSectionAnnouncementRequest("Office hours (test)", "Body", sectionId, DateTime.UtcNow.AddDays(1), null), TestData.Json))
            .ReadAsync<AnnouncementDto>();
        Assert.Equal(AnnouncementStatus.Scheduled, own.Status);

        var mine = (await (await instructor.GetAsync("/api/instructor/announcements?pageSize=100")).ReadAsync<PagedResult<AnnouncementDto>>()).Items;
        Assert.Contains(mine, a => a.Id == own.Id);
        Assert.DoesNotContain(mine, a => a.Id == adminPost.Id);

        var updated = await instructor.PutAsJsonAsync($"/api/instructor/announcements/{own.Id}",
            new SaveSectionAnnouncementRequest("Office hours moved (test)", "Body", sectionId, null, null), TestData.Json);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var dto = await updated.ReadAsync<AnnouncementDto>();
        Assert.Equal(AnnouncementStatus.Live, dto.Status);                 // publish time cleared = publish now
        Assert.NotEmpty(await NotificationsOfAsync(own.Id));               // so the students are notified now

        Assert.Equal(HttpStatusCode.NoContent, (await instructor.DeleteAsync($"/api/instructor/announcements/{own.Id}")).StatusCode);
    }
}
