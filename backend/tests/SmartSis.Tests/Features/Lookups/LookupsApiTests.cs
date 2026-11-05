using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Lookups.Models.Dtos;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Lookups;

/// <summary>Admin lookups for form dropdowns (PB-02, PB-03).</summary>
[Trait("SR", "SR-20")]
[Trait("SR", "SR-21")]
public sealed class LookupsApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Url = "/api/admin/lookups";

    [Fact]
    public async Task Get_ReturnsSeededLists()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);

        var res = await admin.GetAsync(Url);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = (await res.Content.ReadFromJsonAsync<LookupsDto>())!;
        Assert.Contains(body.Campuses, c => c.Name == "Main Campus");
        Assert.Equal(["CSC", "MAT"], body.Departments.Select(d => d.Code).Where(c => c is "CSC" or "MAT"));
        var cs = Assert.Single(body.Majors, m => m.Name == "Computer Science");
        Assert.Equal(18, cs.DefaultMaxCredits);
        Assert.Contains(body.Rooms, r => r.Name == "B-101" && r.CampusName == "Main Campus" && r.Capacity == 40);
        Assert.Contains(body.Instructors, i => i.UniversityId == SisApiFactory.InstructorId);
    }

    [Fact]
    public async Task Get_RoomsOrderedByName()
    {
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);
        var body = (await admin.GetFromJsonAsync<LookupsDto>(Url))!;

        var names = body.Rooms.Select(r => r.Name).ToList();
        Assert.Equal(names.Order(StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task Get_DeactivatedInstructor_NotListed()
    {
        await factory.WithDbAsync(async db =>
        {
            var dept = await db.Set<Department>().FirstAsync(d => d.Code == "CSC");
            var user = new User
            {
                UniversityId = 2999, Email = "inactive.instructor@smartsis.local", FullName = "Inactive Instructor",
                Role = UserRole.Instructor, PasswordHash = "x", IsActive = false,
            };
            db.Add(user);
            db.Add(new Instructor { User = user, DepartmentId = dept.Id });
            await db.SaveChangesAsync();
        });
        var admin = await factory.ClientForAsync(SisApiFactory.AdminId);

        var body = (await admin.GetFromJsonAsync<LookupsDto>(Url))!;

        Assert.DoesNotContain(body.Instructors, i => i.UniversityId == 2999);
    }
}
