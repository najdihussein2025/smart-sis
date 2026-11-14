using System.Net;
using System.Net.Http.Json;
using SmartSis.Api.Features.Cart.Models.Dtos;
using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Cart;

/// <summary>PB-04 / T16: course catalog and cart against the seeded current term. Tests in a class run one at a time.</summary>
public sealed class CatalogCartApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private async Task<HttpClient> StudentAsync()
    {
        await factory.ResetStudentAsync();
        return await factory.ClientForAsync(SisApiFactory.StudentId);
    }

    [Fact]
    public async Task Catalog_DefaultsToStudentMajor_WithMajorAndElectiveTypes()
    {
        var client = await StudentAsync();
        var catalog = await (await client.GetAsync("/api/catalog/courses")).ReadAsync<CatalogDto>();

        Assert.NotNull(catalog.TermId);
        Assert.Equal(["CSC 101", "CSC 210", "CSC 310", "MAT 201"], catalog.Courses.Select(c => c.Code));
        Assert.Equal(CourseType.Elective, catalog.Courses.Single(c => c.Code == "MAT 201").Type);
        Assert.Equal(2, catalog.Courses.Single(c => c.Code == "CSC 210").SectionCount);
        Assert.All(catalog.Courses, c => Assert.Equal(CatalogStatus.Available, c.Status));
    }

    [Theory]
    [InlineData("Major", 3)]
    [InlineData("Elective", 1)]
    public async Task Catalog_TypeTab_FiltersCourses(string type, int expected)
    {
        var client = await StudentAsync();
        var catalog = await (await client.GetAsync($"/api/catalog/courses?type={type}")).ReadAsync<CatalogDto>();
        Assert.Equal(expected, catalog.Courses.Count);
    }

    [Fact]
    public async Task Catalog_Search_MatchesCodeOrName()
    {
        var client = await StudentAsync();
        var catalog = await (await client.GetAsync("/api/catalog/courses?search=database")).ReadAsync<CatalogDto>();
        Assert.Equal("CSC 310", Assert.Single(catalog.Courses).Code);
    }

    [Fact]
    public async Task CourseDetail_ShowsPrerequisitesAndSectionsWithSeats()
    {
        var client = await StudentAsync();
        var id = (await (await client.GetAsync("/api/catalog/courses")).ReadAsync<CatalogDto>()).Courses.Single(c => c.Code == "CSC 210").Id;

        var detail = await (await client.GetAsync($"/api/catalog/courses/{id}")).ReadAsync<CatalogCourseDetailDto>();

        var prereq = Assert.Single(detail.Prerequisites);
        Assert.Equal("CSC 101", prereq.Code);
        Assert.False(prereq.Passed);
        Assert.Equal(["A", "B"], detail.Sections.Select(s => s.SectionCode));
        var a = detail.Sections[0];
        Assert.Equal("Demo Instructor", a.InstructorName);
        Assert.Equal(a.Capacity, a.SeatsLeft);
        Assert.Equal(2, a.Meetings.Count);
    }

    [Fact]
    public async Task CourseDetail_PassedPrerequisite_IsMarked()
    {
        var client = await StudentAsync();
        await factory.AddCompletedAsync(SisApiFactory.StudentId, "CSC 101", 2.0m);
        var id = (await (await client.GetAsync("/api/catalog/courses")).ReadAsync<CatalogDto>()).Courses.Single(c => c.Code == "CSC 210").Id;

        var detail = await (await client.GetAsync($"/api/catalog/courses/{id}")).ReadAsync<CatalogCourseDetailDto>();
        Assert.True(detail.Prerequisites.Single().Passed);
    }

    [Fact]
    public async Task CourseDetail_Unknown_Returns404()
    {
        var client = await StudentAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/catalog/courses/99999")).StatusCode);
    }

    [Fact]
    public async Task Cart_AddThenList_ShowsSectionAndInCartStatus()
    {
        var client = await StudentAsync();
        var sectionId = await factory.SectionIdAsync("CSC 101");

        var res = await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(sectionId));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var cart = await res.ReadAsync<CartDto>();
        var item = Assert.Single(cart.Items);
        Assert.Equal("CSC 101", item.CourseCode);
        Assert.Equal(3, cart.CartCredits);
        Assert.Equal(18, cart.MaxCredits);

        var catalog = await (await client.GetAsync("/api/catalog/courses")).ReadAsync<CatalogDto>();
        Assert.Equal(CatalogStatus.InCart, catalog.Courses.Single(c => c.Code == "CSC 101").Status);
    }

    [Fact]
    [Trait("Rule", "E7")]
    public async Task Cart_SameSectionTwice_Returns409()
    {
        var client = await StudentAsync();
        var sectionId = await factory.SectionIdAsync("CSC 101");
        await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(sectionId));

        var res = await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(sectionId));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains("already_in_cart", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cart_TwoSectionsOfSameCourse_Allowed()
    {
        var client = await StudentAsync();
        await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(await factory.SectionIdAsync("CSC 210", "A")));
        var res = await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(await factory.SectionIdAsync("CSC 210", "B")));
        Assert.Equal(2, (await res.ReadAsync<CartDto>()).Items.Count);
    }

    [Fact]
    public async Task Cart_UnknownSection_Returns404_AndBadId_Returns400()
    {
        var client = await StudentAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(99999))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(0))).StatusCode);
    }

    [Fact]
    public async Task Cart_Remove_EmptiesCart_ThenSecondRemoveIs404()
    {
        var client = await StudentAsync();
        var sectionId = await factory.SectionIdAsync("MAT 201");
        await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(sectionId));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/cart/{sectionId}")).StatusCode);
        Assert.Empty((await (await client.GetAsync("/api/cart")).ReadAsync<CartDto>()).Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/cart/{sectionId}")).StatusCode);
    }

    [Fact]
    public async Task Cart_IsPerStudent()
    {
        var client = await StudentAsync();
        await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(await factory.SectionIdAsync("CSC 101")));

        var other = await factory.ClientForAsync(await factory.AddStudentAsync(3901));
        Assert.Empty((await (await other.GetAsync("/api/cart")).ReadAsync<CartDto>()).Items);
    }
}
