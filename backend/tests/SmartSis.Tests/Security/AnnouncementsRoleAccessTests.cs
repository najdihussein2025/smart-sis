using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for PB-12: announcements and notifications endpoints deny the wrong role (403) and anonymous callers (401).</summary>
public sealed class AnnouncementsRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    // Authorization runs before model binding, so an empty JSON body is enough for POST / PUT.
    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null,
    };

    /// <summary>Any signed-in role may call these.</summary>
    public static TheoryData<string, string> AnyRoleEndpoints => new()
    {
        { "GET", "/api/announcements" },
        { "GET", "/api/notifications" },
        { "GET", "/api/notifications/unread-count" },
        { "POST", "/api/notifications/read-all" },
    };

    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/announcements" },
        { "POST", "/api/admin/announcements" },
        { "PUT", "/api/admin/announcements/1" },
        { "DELETE", "/api/admin/announcements/1" },
    };

    public static TheoryData<string, string> InstructorEndpoints => new()
    {
        { "GET", "/api/instructor/announcements" },
        { "GET", "/api/instructor/announcements/sections" },
        { "POST", "/api/instructor/announcements" },
        { "PUT", "/api/instructor/announcements/1" },
        { "DELETE", "/api/instructor/announcements/1" },
    };

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(AnyRoleEndpoints))]
    [MemberData(nameof(AdminEndpoints))]
    [MemberData(nameof(InstructorEndpoints))]
    public async Task NoToken_Returns401(string method, string url)
    {
        var res = await factory.CreateClient().SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task MarkRead_NoToken_Returns401()
    {
        var res = await factory.CreateClient().SendAsync(Request("POST", "/api/notifications/1/read"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AnyRoleEndpoints))]
    public async Task AnyRole_Allowed(string method, string url)
    {
        foreach (var id in new[] { SisApiFactory.StudentId, SisApiFactory.InstructorId, SisApiFactory.AdminId })
        {
            var client = await factory.ClientForAsync(id);
            var res = await client.SendAsync(Request(method, url));
            Assert.True(res.IsSuccessStatusCode, $"{method} {url} as {id} returned {(int)res.StatusCode}");
        }
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_StudentOrInstructor_Returns403(string method, string url)
    {
        foreach (var id in new[] { SisApiFactory.StudentId, SisApiFactory.InstructorId })
        {
            var client = await factory.ClientForAsync(id);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
        }
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(InstructorEndpoints))]
    public async Task InstructorEndpoint_StudentOrAdmin_Returns403(string method, string url)
    {
        foreach (var id in new[] { SisApiFactory.StudentId, SisApiFactory.AdminId })
        {
            var client = await factory.ClientForAsync(id);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
        }
    }
}
