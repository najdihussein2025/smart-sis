using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for the Grading endpoints (PB-08, PB-09): wrong role → 403, no token → 401.</summary>
public sealed class GradingRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/grade-components?courseId=1&termId=1" },
        { "GET", "/api/admin/grade-components/1" },
        { "POST", "/api/admin/grade-components" },
        { "PUT", "/api/admin/grade-components/1" },
        { "DELETE", "/api/admin/grade-components/1" },
    };

    public static TheoryData<string, string> InstructorEndpoints => new()
    {
    };

    public static TheoryData<string, string> StudentEndpoints => new()
    {
    };

    // Authorization runs before model binding, so an empty JSON body is enough for POST / PUT.
    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null,
    };

    private async Task<HttpStatusCode> SendAsync(long? universityId, string method, string url)
    {
        var client = universityId is { } id ? await factory.ClientForAsync(id) : factory.CreateClient();
        return (await client.SendAsync(Request(method, url))).StatusCode;
    }

    [Theory, Trait("SR", "SR-22"), MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_NoToken_Returns401(string method, string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, await SendAsync(null, method, url));

    [Theory, Trait("SR", "SR-22"), MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_StudentToken_Returns403(string method, string url) =>
        Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(SisApiFactory.StudentId, method, url));

    [Theory, Trait("SR", "SR-22"), MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_InstructorToken_Returns403(string method, string url) =>
        Assert.Equal(HttpStatusCode.Forbidden, await SendAsync(SisApiFactory.InstructorId, method, url));
}
