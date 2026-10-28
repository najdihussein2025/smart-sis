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

    // TODO(phase-2+): wrong role → 403 for every [Authorize(Policy = ...)] controller.
}
