using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for the chatbot (PB-15/PB-19) and knowledge base: wrong role → 403, no token → 401.</summary>
public sealed class ChatRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    public static TheoryData<string, string> StudentEndpoints => new()
    {
        { "POST", "/api/chat/messages" },
        { "GET", "/api/chat/conversations" },
        { "GET", "/api/chat/conversations/1" },
        { "PUT", "/api/chat/conversations/1" },
        { "DELETE", "/api/chat/conversations/1" },
    };

    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/knowledge" },
        { "POST", "/api/admin/knowledge" },
        { "POST", "/api/admin/knowledge/1/reingest" },
        { "DELETE", "/api/admin/knowledge/1" },
    };

    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null,
    };

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(StudentEndpoints))]
    [MemberData(nameof(AdminEndpoints))]
    public async Task NoToken_Returns401(string method, string url)
    {
        var res = await factory.CreateClient().SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [Trait("SR", "SR-22")]
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

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task CurrentTerm_NoToken_Returns401()
    {
        var res = await factory.CreateClient().GetAsync("/api/terms/current");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
