using System.Net;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Security;

/// <summary>SR-22 for the dashboard endpoints (PB-22): no token → 401, another role → 403, own role → 200.</summary>
public sealed class DashboardRoleAccessTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    public static TheoryData<string, long> Endpoints => new()
    {
        { "/api/dashboard/student", SisApiFactory.StudentId },
        { "/api/dashboard/instructor", SisApiFactory.InstructorId },
        { "/api/dashboard/admin", SisApiFactory.AdminId },
    };

    private static readonly long[] Everyone = [SisApiFactory.StudentId, SisApiFactory.InstructorId, SisApiFactory.AdminId];

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(Endpoints))]
    public async Task Dashboard_NoToken_Returns401(string url, long _)
    {
        var res = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(Endpoints))]
    public async Task Dashboard_OtherRoles_Return403(string url, long owner)
    {
        foreach (var who in Everyone.Where(w => w != owner))
        {
            var client = await factory.ClientForAsync(who);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(url)).StatusCode);
        }
    }

    [Theory]
    [Trait("SR", "SR-22")]
    [MemberData(nameof(Endpoints))]
    public async Task Dashboard_OwnRole_Returns200(string url, long owner)
    {
        var client = await factory.ClientForAsync(owner);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
    }
}
