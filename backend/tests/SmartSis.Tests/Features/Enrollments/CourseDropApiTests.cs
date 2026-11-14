using System.Net;
using System.Net.Http.Json;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Rules;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Enrollments;

/// <summary>PB-07 / T26: dropping a course returns the seat, works only in add/drop and only on the student's own enrollment.</summary>
public sealed class CourseDropApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private async Task<(HttpClient Client, int SectionId)> EnrolledInMat201Async(long universityId = SisApiFactory.StudentId)
    {
        await factory.ResetStudentAsync(universityId);
        await factory.SetWindowAsync(TermWindowState.AddDrop);
        var client = await factory.ClientForAsync(universityId);
        var sectionId = await factory.SectionIdAsync("MAT 201");
        var res = await client.PostAsJsonAsync("/api/enrollment/enroll", new EnrollRequest([sectionId]));
        Assert.True((await res.ReadAsync<EnrollResultDto>()).Results[0].Success);
        return (client, sectionId);
    }

    private static async Task<int> SeatsLeftAsync(HttpClient client, int sectionId)
    {
        var catalog = await (await client.GetAsync("/api/catalog/courses")).ReadAsync<CatalogDto>();
        var courseId = catalog.Courses.Single(c => c.Code == "MAT 201").Id;
        var detail = await (await client.GetAsync($"/api/catalog/courses/{courseId}")).ReadAsync<CatalogCourseDetailDto>();
        return detail.Sections.Single(s => s.Id == sectionId).SeatsLeft;
    }

    [Fact]
    [Trait("Rule", "E8")]
    public async Task Drop_ReturnsTheSeatToTheSection()
    {
        var (client, sectionId) = await EnrolledInMat201Async();
        var before = await SeatsLeftAsync(client, sectionId);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(sectionId))).StatusCode);

        Assert.Equal(before + 1, await SeatsLeftAsync(client, sectionId));
    }

    [Fact]
    public async Task Drop_ThenEnrollAgain_IsAllowed()
    {
        var (client, sectionId) = await EnrolledInMat201Async();
        await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(sectionId));

        var again = await (await client.PostAsJsonAsync("/api/enrollment/enroll", new EnrollRequest([sectionId]))).ReadAsync<EnrollResultDto>();

        Assert.True(again.Results[0].Success);
    }

    [Fact]
    public async Task Drop_Twice_SecondIs404()
    {
        var (client, sectionId) = await EnrolledInMat201Async();
        await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(sectionId));
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(sectionId))).StatusCode);
    }

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task Drop_AnotherStudentsSection_Is404_AndTheirEnrollmentStays()
    {
        var (owner, sectionId) = await EnrolledInMat201Async(await factory.AddStudentAsync(3903));
        await factory.ResetStudentAsync();
        var me = await factory.ClientForAsync(SisApiFactory.StudentId);

        Assert.Equal(HttpStatusCode.NotFound, (await me.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(sectionId))).StatusCode);
        Assert.Single((await (await owner.GetAsync("/api/enrollment/my")).ReadAsync<MyEnrollmentsDto>()).Items);
    }

    [Fact]
    [Trait("Rule", "E1")]
    public async Task Drop_AfterAddDropCloses_Returns409()
    {
        var (client, sectionId) = await EnrolledInMat201Async();
        await factory.SetWindowAsync(TermWindowState.Closed);

        var res = await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(sectionId));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains(EnrollmentPolicy.DropWindowClosed, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Drop_BadSectionId_Returns400()
    {
        await factory.ResetStudentAsync();
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(0))).StatusCode);
    }
}
