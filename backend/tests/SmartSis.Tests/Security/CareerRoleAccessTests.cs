using System.Net;
using System.Text;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for the career module: student endpoints need a student, the question bank needs an admin.</summary>
[Trait("SR", "SR-22")]
public sealed class CareerRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    public static TheoryData<string, string> StudentEndpoints => new()
    {
        { "GET", "/api/career/assessments" },
        { "POST", "/api/career/assessments" },
        { "GET", "/api/career/quiz" },
        { "GET", "/api/career/recommendations" },
        { "GET", "/api/career/careers" },
        { "GET", "/api/career/careers/1/roadmap" },
    };

    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/career/traits" },
        { "POST", "/api/admin/career/traits" },
        { "PUT", "/api/admin/career/traits/1" },
        { "GET", "/api/admin/career/questions" },
        { "GET", "/api/admin/career/questions/1" },
        { "POST", "/api/admin/career/questions" },
        { "PUT", "/api/admin/career/questions/1" },
        { "DELETE", "/api/admin/career/questions/1" },
        { "GET", "/api/admin/career/careers" },
        { "GET", "/api/admin/career/careers/1" },
        { "POST", "/api/admin/career/careers" },
        { "PUT", "/api/admin/career/careers/1" },
        { "GET", "/api/admin/career/course-tags" },
        { "PUT", "/api/admin/career/course-tags/1" },
    };

    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", Encoding.UTF8, "application/json") : null,
    };

    [Theory]
    [MemberData(nameof(StudentEndpoints))]
    [MemberData(nameof(AdminEndpoints))]
    public async Task NoToken_Returns401(string method, string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().SendAsync(Request(method, url))).StatusCode);

    [Theory]
    [MemberData(nameof(StudentEndpoints))]
    public async Task StudentEndpoint_InstructorOrAdmin_Returns403(string method, string url)
    {
        foreach (var id in new[] { SisApiFactory.InstructorId, SisApiFactory.AdminId })
        {
            var client = await factory.ClientForAsync(id);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
        }
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminEndpoint_StudentOrInstructor_Returns403(string method, string url)
    {
        foreach (var id in new[] { SisApiFactory.StudentId, SisApiFactory.InstructorId })
        {
            var client = await factory.ClientForAsync(id);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
        }
    }
}
