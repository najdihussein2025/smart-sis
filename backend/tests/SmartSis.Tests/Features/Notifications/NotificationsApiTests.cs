using System.Net;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Dtos;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Notifications;

/// <summary>PB-12 / T48: the bell — my notifications, unread count, mark read. Only ever the caller's own rows.</summary>
public sealed class NotificationsApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const long Student = 3005;

    private async Task<int> AddAsync(long universityId, string title, DateTime? readAt = null)
    {
        var userId = await factory.UserIdAsync(universityId);
        var id = 0;
        await factory.WithDbAsync(async db =>
        {
            var n = new Notification { UserId = userId, Type = NotificationType.General, Title = title, Body = "Simulated.", Link = "/student", ReadAt = readAt };
            db.Add(n);
            await db.SaveChangesAsync();
            id = n.Id;
        });
        return id;
    }

    private static async Task<int> UnreadAsync(HttpClient client) =>
        (await (await client.GetAsync("/api/notifications/unread-count")).ReadAsync<UnreadCountDto>()).Count;

    [Fact]
    public async Task List_ReturnsOnlyMine_NewestFirst()
    {
        await AddAsync(Student, "Older (test)");
        await AddAsync(Student, "Newer (test)");
        await AddAsync(SisApiFactory.StudentId, "Someone else's (test)");
        var client = await factory.ClientForAsync(Student);

        var page = await (await client.GetAsync("/api/notifications?pageSize=100")).ReadAsync<PagedResult<NotificationDto>>();

        var titles = page.Items.Select(n => n.Title).ToList();
        Assert.DoesNotContain("Someone else's (test)", titles);
        Assert.True(titles.IndexOf("Newer (test)") < titles.IndexOf("Older (test)"));
        Assert.Equal(page.Items.OrderByDescending(n => n.CreatedAt).Select(n => n.Id), page.Items.Select(n => n.Id));
    }

    [Fact]
    public async Task UnreadOnly_And_MarkRead_UpdateTheCount()
    {
        var client = await factory.ClientForAsync(Student);
        var id = await AddAsync(Student, "Read me (test)");
        await AddAsync(Student, "Already read (test)", DateTime.UtcNow.AddHours(-1));
        var before = await UnreadAsync(client);

        var unread = await (await client.GetAsync("/api/notifications?unreadOnly=true&pageSize=100")).ReadAsync<PagedResult<NotificationDto>>();
        Assert.All(unread.Items, n => Assert.Null(n.ReadAt));
        Assert.DoesNotContain(unread.Items, n => n.Title == "Already read (test)");

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        Assert.Equal(before - 1, await UnreadAsync(client));

        // Reading it again is harmless.
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/notifications/{id}/read", null)).StatusCode);
        Assert.Equal(before - 1, await UnreadAsync(client));
    }

    [Fact]
    public async Task MarkRead_SomeoneElsesNotification_Returns404()
    {
        var othersId = await AddAsync(SisApiFactory.StudentId, "Not yours (test)");
        var client = await factory.ClientForAsync(Student);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/api/notifications/{othersId}/read", null)).StatusCode);
    }

    [Fact]
    public async Task ReadAll_ClearsMyCountOnly()
    {
        await AddAsync(Student, "One (test)");
        await AddAsync(Student, "Two (test)");
        await AddAsync(SisApiFactory.StudentId, "Untouched (test)");
        var client = await factory.ClientForAsync(Student);
        var other = await factory.ClientForAsync(SisApiFactory.StudentId);
        var otherBefore = await UnreadAsync(other);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/notifications/read-all", null)).StatusCode);

        Assert.Equal(0, await UnreadAsync(client));
        Assert.Equal(otherBefore, await UnreadAsync(other));
    }

    [Fact]
    public async Task SeededDemo_StudentHasAnnouncementNotifications_OldestRead()
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        var page = await (await client.GetAsync("/api/notifications?pageSize=100")).ReadAsync<PagedResult<NotificationDto>>();

        var announcements = page.Items.Where(n => n.Type == NotificationType.Announcement).ToList();
        Assert.Contains(announcements, n => n.Title == "Welcome to the new Smart SIS" && n.ReadAt != null);
        Assert.Contains(announcements, n => n.Title == "CSC 210 A: lab moved this week" && n.ReadAt == null);
        Assert.All(announcements, n => Assert.StartsWith("/student/announcements?announcement=", n.Link));
    }
}
