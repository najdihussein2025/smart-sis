using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for the attendance endpoints (PB-10, PB-11): no token → 401, wrong role → 403.</summary>
public sealed class AttendanceRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    public static TheoryData<string, string> InstructorEndpoints => new()
    {
        { "GET", "/api/attendance/teaching" },
        { "GET", "/api/attendance/sheet?meetingId=1&date=2026-01-05" },
        { "GET", "/api/attendance/sections/1/sessions" },
        { "GET", "/api/attendance/sessions/1" },
        { "POST", "/api/attendance/sessions" },
    };

    // Authorization runs before model binding, so an empty JSON body is enough for POST.
    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null,
    };

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(InstructorEndpoints))]
    public async Task NoToken_Returns401(string method, string url)
    {
        var res = await factory.CreateClient().SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(InstructorEndpoints))]
    public async Task InstructorEndpoint_StudentToken_Returns403(string method, string url)
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(InstructorEndpoints))]
    public async Task InstructorEndpoint_AdminToken_Returns403(string method, string url)
    {
        var client = await factory.ClientForAsync(SisApiFactory.AdminId);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
    }
}
