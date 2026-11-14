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
/// Student support API (PB-13, T50). Each test uses its own new simulated student, so the seeded requests
/// (SupportDevSeed) and the other tests' requests never show up in its lists.
/// </summary>
public sealed class SupportApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/support/requests";
    private static int _next = 41000;

    private async Task<(HttpClient Client, int UserId)> NewStudentAsync()
    {
        var uni = await factory.AddStudentAsync(Interlocked.Increment(ref _next));
        return (await factory.ClientForAsync(uni), await factory.UserIdAsync(uni));
    }

    private async Task<int> CategoryIdAsync(string name = "Registration")
    {
        var id = 0;
        await factory.WithDbAsync(async db => id = await db.Set<SupportCategory>().Where(c => c.Name == name).Select(c => c.Id).SingleAsync());
        return id;
    }

    private async Task<SupportRequestDetailDto> SubmitAsync(HttpClient client, string subject = "Section question")
    {
        var res = await client.PostAsJsonAsync(Url, new CreateSupportRequestRequest(await CategoryIdAsync(), subject, "Simulated description."));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await res.ReadAsync<SupportRequestDetailDto>();
    }

    private Task SetStatusAsync(int requestId, SupportStatus status, int? assignee = null) =>
        factory.WithDbAsync(async db =>
        {
            var r = await db.Set<SupportRequest>().SingleAsync(x => x.Id == requestId);
            r.Status = status;
            r.AssignedAdminId = assignee ?? r.AssignedAdminId;
            r.ResolvedAt = status is SupportStatus.Resolved or SupportStatus.Closed ? DateTime.UtcNow : null;
            await db.SaveChangesAsync();
        });

    [Fact]
    public async Task Categories_ReturnsSeededCategoriesByName()
    {
        var (client, _) = await NewStudentAsync();
        var categories = await (await client.GetAsync("/api/support/categories")).ReadAsync<List<SupportCategoryDto>>();
        Assert.Contains(categories, c => c.Name == "Grade dispute");
        Assert.Equal(categories.Select(c => c.Name).Order(), categories.Select(c => c.Name));
    }

    [Fact]
    public async Task Create_Valid_Returns201OpenAndUnassigned()
    {
        var (client, userId) = await NewStudentAsync();
        var res = await client.PostAsJsonAsync(Url, new CreateSupportRequestRequest(await CategoryIdAsync("Finance"), "  Charge looks wrong  ", "The tuition charge is higher than expected."));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.ReadAsync<SupportRequestDetailDto>();
        Assert.Equal("Charge looks wrong", created.Subject);
        Assert.Equal("Finance", created.CategoryName);
        Assert.Equal(SupportStatus.Open, created.Status);
        Assert.Empty(created.Messages);
        Assert.EndsWith($"{Url}/{created.Id}", res.Headers.Location!.ToString());

        await factory.WithDbAsync(async db =>
        {
            var row = await db.Set<SupportRequest>().SingleAsync(r => r.Id == created.Id);
            Assert.Equal(userId, row.StudentId); // identity from the token
            Assert.Null(row.AssignedAdminId);
        });
    }

    [Fact]
    public async Task Create_UnknownCategory_Returns400OnCategoryId()
    {
        var (client, _) = await NewStudentAsync();
        var res = await client.PostAsJsonAsync(Url, new CreateSupportRequestRequest(99999, "Subject", "Description"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("categoryId", problem.Errors.Keys);
    }

    [Fact]
    public async Task Create_BlankSubjectAndTooLongDescription_Returns400()
    {
        var (client, _) = await NewStudentAsync();
        var res = await client.PostAsJsonAsync(Url, new CreateSupportRequestRequest(await CategoryIdAsync(), "   ", new string('x', 4001)));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("subject", problem.Errors.Keys);
        Assert.Contains("description", problem.Errors.Keys);
    }

    [Fact]
    public async Task List_ReturnsOnlyMine_NewestFirst_FilteredByStatus()
    {
        var (client, _) = await NewStudentAsync();
        var (other, _) = await NewStudentAsync();
        var first = await SubmitAsync(client, "First");
        var second = await SubmitAsync(client, "Second");
        await SubmitAsync(other, "Someone else's");
        await SetStatusAsync(first.Id, SupportStatus.Resolved);

        var all = await (await client.GetAsync(Url)).ReadAsync<PagedResult<SupportRequestSummaryDto>>();
        Assert.Equal(["Second", "First"], all.Items.Select(i => i.Subject));
        Assert.Equal(2, all.TotalCount);

        var resolved = await (await client.GetAsync($"{Url}?status=Resolved")).ReadAsync<PagedResult<SupportRequestSummaryDto>>();
        Assert.Equal(first.Id, Assert.Single(resolved.Items).Id);
        Assert.DoesNotContain(resolved.Items, i => i.Id == second.Id);
    }

    [Fact]
    public async Task Get_AnotherStudentsRequest_Returns404()
    {
        var (owner, _) = await NewStudentAsync();
        var (other, _) = await NewStudentAsync();
        var request = await SubmitAsync(owner);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"{Url}/{request.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"{Url}/{request.Id}")).StatusCode);
        var reply = await other.PostAsJsonAsync($"{Url}/{request.Id}/messages", new SupportReplyRequest("Hi"));
        Assert.Equal(HttpStatusCode.NotFound, reply.StatusCode);
    }

    [Fact]
    public async Task Reply_AddsMessageToThread_AndNotifiesAssignedAdmin()
    {
        var (client, userId) = await NewStudentAsync();
        var request = await SubmitAsync(client);
        var adminId = await factory.UserIdAsync(SisApiFactory.AdminId);
        await SetStatusAsync(request.Id, SupportStatus.InProgress, adminId);

        var res = await client.PostAsJsonAsync($"{Url}/{request.Id}/messages", new SupportReplyRequest("  More details here.  "));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await res.ReadAsync<SupportRequestDetailDto>();
        var message = Assert.Single(detail.Messages);
        Assert.Equal("More details here.", message.Body);
        Assert.Equal(userId, message.AuthorId);
        Assert.Equal(UserRole.Student, message.AuthorRole);
        Assert.Equal(SupportStatus.InProgress, detail.Status);
        await factory.WithDbAsync(async db =>
            Assert.True(await db.Set<Notification>().AnyAsync(n => n.UserId == adminId && n.Type == NotificationType.Support
                                                                    && n.Link == $"/admin/support/{request.Id}")));
    }

    [Fact]
    public async Task Reply_OnResolved_ReopensRequest()
    {
        var (client, _) = await NewStudentAsync();
        var request = await SubmitAsync(client);
        await SetStatusAsync(request.Id, SupportStatus.Resolved);

        var detail = await (await client.PostAsJsonAsync($"{Url}/{request.Id}/messages", new SupportReplyRequest("Still broken."))).ReadAsync<SupportRequestDetailDto>();

        Assert.Equal(SupportStatus.Open, detail.Status);
        Assert.Null(detail.ResolvedAt);
    }

    [Fact]
    public async Task Reply_OnClosed_Returns409()
    {
        var (client, _) = await NewStudentAsync();
        var request = await SubmitAsync(client);
        await SetStatusAsync(request.Id, SupportStatus.Closed);

        var res = await client.PostAsJsonAsync($"{Url}/{request.Id}/messages", new SupportReplyRequest("Hello?"));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.ReadAsync<ProblemDetails>();
        Assert.Equal("request_closed", problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task Reply_Blank_Returns400()
    {
        var (client, _) = await NewStudentAsync();
        var request = await SubmitAsync(client);
        var res = await client.PostAsJsonAsync($"{Url}/{request.Id}/messages", new SupportReplyRequest(" "));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task SeededDemoStudent_HasRequestsWithThreads()
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        var list = await (await client.GetAsync(Url)).ReadAsync<PagedResult<SupportRequestSummaryDto>>();
        Assert.True(list.TotalCount >= 3);
        var withThread = list.Items.First(i => i.MessageCount > 0);
        var detail = await (await client.GetAsync($"{Url}/{withThread.Id}")).ReadAsync<SupportRequestDetailDto>();
        Assert.Contains(detail.Messages, m => m.AuthorRole == UserRole.Admin);
    }
}
