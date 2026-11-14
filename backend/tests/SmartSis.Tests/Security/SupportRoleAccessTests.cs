using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for the support endpoints (PB-13, PB-14): wrong role → 403, no token → 401.</summary>
[Trait("SR", "SR-22")]
public sealed class SupportRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    /// <summary>Student-only support routes.</summary>
    public static TheoryData<string, string> StudentEndpoints => new()
    {
        { "GET", "/api/support/categories" },
        { "GET", "/api/support/requests" },
        { "POST", "/api/support/requests" },
        { "GET", "/api/support/requests/1" },
        { "POST", "/api/support/requests/1/messages" },
    };

    [Theory]
    [MemberData(nameof(StudentEndpoints))]
    public async Task StudentEndpoint_NoToken_Returns401(string method, string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().SendAsync(Request(method, url))).StatusCode);

    [Theory]
    [MemberData(nameof(StudentEndpoints))]
    public async Task StudentEndpoint_InstructorToken_Returns403(string method, string url)
    {
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
    }

    [Theory]
    [MemberData(nameof(StudentEndpoints))]
    public async Task StudentEndpoint_AdminToken_Returns403(string method, string url)
    {
        var client = await factory.ClientForAsync(SisApiFactory.AdminId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
    }

    // Authorization runs before model binding, so an empty JSON body is enough for POST / PUT.
    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null,
    };
}
