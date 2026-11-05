using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22: unauthorized access is denied. Add a row for every new endpoint.</summary>
public sealed class RoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    public static TheoryData<string> AuthenticatedEndpoints => new()
    {
        "/api/auth/me",
        "/api/profile",
    };

    [Theory]
    [MemberData(nameof(AuthenticatedEndpoints))]
    public async Task NoToken_Returns401(string url)
    {
        var res = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [InlineData(SisApiFactory.StudentId)]
    [InlineData(SisApiFactory.InstructorId)]
    [InlineData(SisApiFactory.AdminId)]
    public async Task Profile_AnyRole_Returns200(long universityId)
    {
        var client = await factory.ClientForAsync(universityId);
        var res = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Health_IsPublic()
    {
        var res = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    /// <summary>Every admin-only route. Add a row for each new one.</summary>
    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/terms" },
        { "GET", "/api/admin/terms/1" },
        { "POST", "/api/admin/terms" },
        { "PUT", "/api/admin/terms/1" },
        { "GET", "/api/admin/courses" },
        { "GET", "/api/admin/courses/1" },
        { "POST", "/api/admin/courses" },
        { "PUT", "/api/admin/courses/1" },
        { "GET", "/api/admin/users" },
        { "GET", "/api/admin/users/1" },
        { "POST", "/api/admin/users" },
        { "PUT", "/api/admin/users/1" },
        { "POST", "/api/admin/users/1/deactivate" },
        { "GET", "/api/admin/lookups" },
    };

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_NoToken_Returns401(string method, string url)
    {
        var res = await factory.CreateClient().SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_StudentToken_Returns403(string method, string url)
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        var res = await client.SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_InstructorToken_Returns403(string method, string url)
    {
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var res = await client.SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // Authorization runs before model binding, so an empty JSON body is enough for POST / PUT.
    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null,
    };

    // TODO(phase-3): wrong role → 403 for student and instructor controllers as they are added.
}
