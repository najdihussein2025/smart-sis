using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Shared.Models;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Support;

/// <summary>
/// Admin support inbox and categories (PB-14, T53, T54). Each test files its requests for a new simulated student with
/// a unique subject, and searches by it, so the seeded requests and other tests don't get in the way.
/// </summary>
public sealed class AdminSupportApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/support";
    private static int _next = 42000;

    private static string Unique(string prefix) => $"{prefix} {Interlocked.Increment(ref _next)}";

    private Task<HttpClient> AdminAsync() => factory.ClientForAsync(SisApiFactory.AdminId);

    /// <summary>A new student with one request in <paramref name="status"/>. Returns the request id and the student's university id.</summary>
    private async Task<(int Id, long StudentUni)> NewRequestAsync(
        string subject, SupportStatus status = SupportStatus.Open, string category = "Registration", int? assignee = null)
    {
        var uni = await factory.AddStudentAsync(Interlocked.Increment(ref _next));
        var studentId = await factory.UserIdAsync(uni);
        var id = 0;
        await factory.WithDbAsync(async db =>
        {
            var r = new SupportRequest
            {
                StudentId = studentId, CategoryId = await db.Set<SupportCategory>().Where(c => c.Name == category).Select(c => c.Id).SingleAsync(),
                Subject = subject, Description = "Simulated description.", Status = status, AssignedAdminId = assignee,
            };
            db.Add(r);
            await db.SaveChangesAsync();
            id = r.Id;
        });
        return (id, uni);
    }

    private async Task<PagedResult<AdminSupportRequestDto>> SearchAsync(HttpClient client, string query) =>
        await (await client.GetAsync($"{Url}?{query}")).ReadAsync<PagedResult<AdminSupportRequestDto>>();

    [Fact]
    public async Task List_FiltersByStatusCategoryAssigneeAndSearch()
    {
        var client = await AdminAsync();
        var adminId = await factory.UserIdAsync(SisApiFactory.AdminId);
        var tag = Unique("Inbox filter");
        var open = await NewRequestAsync($"{tag} open");
        var mine = await NewRequestAsync($"{tag} mine", SupportStatus.InProgress, "Finance", adminId);
        var resolved = await NewRequestAsync($"{tag} resolved", SupportStatus.Resolved);

        var all = await SearchAsync(client, $"search={Uri.EscapeDataString(tag)}");
        Assert.Equal(3, all.TotalCount);
        var row = all.Items.Single(i => i.Id == mine.Id);
        Assert.Equal("Demo Admin", row.AssignedAdminName);
        Assert.Equal("Finance", row.CategoryName);
        Assert.Equal(mine.StudentUni, row.StudentUniversityId);

        Assert.Equal([resolved.Id], (await SearchAsync(client, $"search={Uri.EscapeDataString(tag)}&status=Resolved")).Items.Select(i => i.Id));
        Assert.Equal([mine.Id], (await SearchAsync(client, $"search={Uri.EscapeDataString(tag)}&assigned=Me")).Items.Select(i => i.Id));
        Assert.Equal(
            new[] { open.Id, resolved.Id }.Order(),
            (await SearchAsync(client, $"search={Uri.EscapeDataString(tag)}&assigned=Unassigned")).Items.Select(i => i.Id).Order());
        var finance = await factory.WithCategoryIdAsync("Finance");
        Assert.Equal([mine.Id], (await SearchAsync(client, $"search={Uri.EscapeDataString(tag)}&categoryId={finance}")).Items.Select(i => i.Id));

        // Search also matches the student's university id.
        Assert.Equal([open.Id], (await SearchAsync(client, $"search={open.StudentUni}")).Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Get_ReturnsStudentAndThread_UnknownIs404()
    {
        var client = await AdminAsync();
        var (id, uni) = await NewRequestAsync(Unique("Detail"));

        var detail = await (await client.GetAsync($"{Url}/{id}")).ReadAsync<AdminSupportRequestDetailDto>();
        Assert.Equal($"student{uni}@smartsis.local", detail.StudentEmail);
        Assert.Equal("Simulated description.", detail.Description);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{Url}/999999")).StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_Resolved_SetsResolvedAt_AndNotifiesAndEmailsStudent()
    {
        var client = await AdminAsync();
        var (id, uni) = await NewRequestAsync(Unique("Status"));
        var studentId = await factory.UserIdAsync(uni);

        var res = await client.PutAsJsonAsync($"{Url}/{id}/status", new ChangeSupportStatusRequest(SupportStatus.Resolved), TestData.Json);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await res.ReadAsync<AdminSupportRequestDetailDto>();
        Assert.Equal(SupportStatus.Resolved, detail.Status);
        Assert.NotNull(detail.ResolvedAt);

        await factory.WithDbAsync(async db =>
        {
            Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == studentId && n.Type == NotificationType.Support
                                                                    && n.Link == $"/student/support/{id}"));
            Assert.True(await db.Set<EmailOutboxMessage>().AnyAsync(e => e.ToEmail == $"student{uni}@smartsis.local"));
        });

        var reopened = await (await client.PutAsJsonAsync($"{Url}/{id}/status", new ChangeSupportStatusRequest(SupportStatus.InProgress), TestData.Json))
            .ReadAsync<AdminSupportRequestDetailDto>();
        Assert.Null(reopened.ResolvedAt);
    }

    [Fact]
    public async Task ChangeStatus_SameStatus_DoesNotNotify()
    {
        var client = await AdminAsync();
        var (id, uni) = await NewRequestAsync(Unique("Same status"));
        var studentId = await factory.UserIdAsync(uni);

        var res = await client.PutAsJsonAsync($"{Url}/{id}/status", new ChangeSupportStatusRequest(SupportStatus.Open), TestData.Json);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        await factory.WithDbAsync(async db => Assert.False(await db.Set<Notification>().AnyAsync(n => n.UserId == studentId)));
    }

    [Fact]
    public async Task ChangeStatus_UnknownStatus_Returns400()
    {
        var client = await AdminAsync();
        var (id, _) = await NewRequestAsync(Unique("Bad status"));
        var res = await client.PutAsync($"{Url}/{id}/status", new StringContent("""{"status":"Archived"}""", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Assign_ToAdmin_ThenUnassign()
    {
        var client = await AdminAsync();
        var adminId = await factory.UserIdAsync(SisApiFactory.AdminId);
        var (id, _) = await NewRequestAsync(Unique("Assign"));

        var assigned = await (await client.PutAsJsonAsync($"{Url}/{id}/assignee", new AssignSupportRequestRequest(adminId))).ReadAsync<AdminSupportRequestDetailDto>();
        Assert.Equal(adminId, assigned.AssignedAdminId);
        Assert.Equal("Demo Admin", assigned.AssignedAdminName);

        var cleared = await (await client.PutAsJsonAsync($"{Url}/{id}/assignee", new AssignSupportRequestRequest(null))).ReadAsync<AdminSupportRequestDetailDto>();
        Assert.Null(cleared.AssignedAdminId);
    }

    [Fact]
    public async Task Assign_ToNonAdmin_Returns400OnAdminId()
    {
        var client = await AdminAsync();
        var (id, uni) = await NewRequestAsync(Unique("Assign student"));
        var res = await client.PutAsJsonAsync($"{Url}/{id}/assignee", new AssignSupportRequestRequest(await factory.UserIdAsync(uni)));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("adminId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task Reply_AddsMessage_MovesOpenToInProgress_AndNotifiesAndEmailsStudent()
    {
        var client = await AdminAsync();
        var (id, uni) = await NewRequestAsync(Unique("Reply"));
        var studentId = await factory.UserIdAsync(uni);

        var res = await client.PostAsJsonAsync($"{Url}/{id}/messages", new SupportReplyRequest("We're on it."));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await res.ReadAsync<AdminSupportRequestDetailDto>();
        var message = Assert.Single(detail.Messages);
        Assert.Equal(UserRole.Admin, message.AuthorRole);
        Assert.Equal(SupportStatus.InProgress, detail.Status);
        await factory.WithDbAsync(async db =>
        {
            Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == studentId && n.Title == "New reply to your support request"));
            var email = await db.Set<EmailOutboxMessage>().SingleAsync(e => e.ToEmail == $"student{uni}@smartsis.local");
            Assert.Contains("We&#39;re on it.", email.Body); // HTML-encoded
        });

        // The student sees the admin's reply in their own view.
        var student = await factory.ClientForAsync(uni);
        var mine = await (await student.GetAsync($"/api/support/requests/{id}")).ReadAsync<SupportRequestDetailDto>();
        Assert.Equal("We're on it.", Assert.Single(mine.Messages).Body);
    }

    [Fact]
    public async Task Reply_OnClosed_Returns409()
    {
        var client = await AdminAsync();
        var (id, _) = await NewRequestAsync(Unique("Closed"), SupportStatus.Closed);
        var res = await client.PostAsJsonAsync($"{Url}/{id}/messages", new SupportReplyRequest("Hello"));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("request_closed", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Admins_ListsActiveAdmins()
    {
        var client = await AdminAsync();
        var admins = await (await client.GetAsync($"{Url}/admins")).ReadAsync<List<SupportAdminDto>>();
        Assert.Contains(admins, a => a.UniversityId == SisApiFactory.AdminId);
    }

    // Categories

    private const string Categories = "/api/admin/support/categories";

    [Fact]
    public async Task Categories_ListWithRequestCounts()
    {
        var client = await AdminAsync();
        var list = await (await client.GetAsync(Categories)).ReadAsync<List<AdminSupportCategoryDto>>();
        Assert.True(list.Single(c => c.Name == "Registration").RequestCount >= 1); // seeded
        Assert.Equal(list.Select(c => c.Name).Order(), list.Select(c => c.Name));
    }

    [Fact]
    public async Task Categories_CreateRenameDeleteUnused()
    {
        var client = await AdminAsync();
        var name = Unique("Housing");

        var res = await client.PostAsJsonAsync(Categories, new SaveSupportCategoryRequest($"  {name}  "));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.ReadAsync<AdminSupportCategoryDto>();
        Assert.Equal(name, created.Name);

        var renamed = await (await client.PutAsJsonAsync($"{Categories}/{created.Id}", new SaveSupportCategoryRequest($"{name} b"))).ReadAsync<AdminSupportCategoryDto>();
        Assert.Equal($"{name} b", renamed.Name);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{Categories}/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{Categories}/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Categories_DuplicateName_Returns409()
    {
        var client = await AdminAsync();
        var res = await client.PostAsJsonAsync(Categories, new SaveSupportCategoryRequest("finance"));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("duplicate_category", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]?.ToString());

        var other = await (await client.PostAsJsonAsync(Categories, new SaveSupportCategoryRequest(Unique("Library")))).ReadAsync<AdminSupportCategoryDto>();
        var rename = await client.PutAsJsonAsync($"{Categories}/{other.Id}", new SaveSupportCategoryRequest("Technical"));
        Assert.Equal(HttpStatusCode.Conflict, rename.StatusCode);
    }

    [Fact]
    public async Task Categories_DeleteInUse_Returns409()
    {
        var client = await AdminAsync();
        var id = await factory.WithCategoryIdAsync("Registration");
        var res = await client.DeleteAsync($"{Categories}/{id}");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("category_in_use", (await res.ReadAsync<ProblemDetails>()).Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Categories_BlankName_Returns400_UnknownIs404()
    {
        var client = await AdminAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Categories, new SaveSupportCategoryRequest(" "))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"{Categories}/999999", new SaveSupportCategoryRequest("X"))).StatusCode);
    }
}

internal static class SupportTestData
{
    public static async Task<int> WithCategoryIdAsync(this SisApiFactory f, string name)
    {
        var id = 0;
        await f.WithDbAsync(async db => id = await db.Set<SupportCategory>().Where(c => c.Name == name).Select(c => c.Id).SingleAsync());
        return id;
    }
}
