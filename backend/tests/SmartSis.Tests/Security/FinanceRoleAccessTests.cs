using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for the finance endpoints (PB-18): no token → 401, wrong role → 403.</summary>
public sealed class FinanceRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    public static TheoryData<string, string> AdminEndpoints => new()
    {
        { "GET", "/api/admin/finance/options" },
        { "GET", "/api/admin/finance/batches" },
        { "GET", "/api/admin/finance/batches/1" },
        { "POST", "/api/admin/finance/batches" },
        { "GET", "/api/admin/finance/charges" },
        { "GET", "/api/admin/finance/charges/1" },
        { "POST", "/api/admin/finance/charges" },
        { "POST", "/api/admin/finance/charges/1/payments" },
    };

    public static TheoryData<string, string> StudentEndpoints => new()
    {
        { "GET", "/api/finance/my" },
    };

    // Authorization runs before model binding, so an empty JSON body is enough for POST.
    private static HttpRequestMessage Request(string method, string url) => new(new HttpMethod(method), url)
    {
        Content = method is "POST" or "PUT" ? new StringContent("{}", System.Text.Encoding.UTF8, "application/json") : null,
    };

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(AdminEndpoints))]
    [MemberData(nameof(StudentEndpoints))]
    public async Task FinanceEndpoint_NoToken_Returns401(string method, string url)
    {
        var res = await factory.CreateClient().SendAsync(Request(method, url));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(AdminEndpoints))]
    public async Task AdminFinanceEndpoint_StudentOrInstructor_Returns403(string method, string url)
    {
        foreach (var who in new[] { SisApiFactory.StudentId, SisApiFactory.InstructorId })
        {
            var client = await factory.ClientForAsync(who);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
        }
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(StudentEndpoints))]
    public async Task StudentFinanceEndpoint_InstructorOrAdmin_Returns403(string method, string url)
    {
        foreach (var who in new[] { SisApiFactory.InstructorId, SisApiFactory.AdminId })
        {
            var client = await factory.ClientForAsync(who);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Request(method, url))).StatusCode);
        }
    }
}
