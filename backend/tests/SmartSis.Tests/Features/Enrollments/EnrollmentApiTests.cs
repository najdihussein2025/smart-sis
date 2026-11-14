using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Cart.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Enrollments.Rules;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Enrollments;

/// <summary>PB-05 / PB-07: enroll from the cart, swap and drop against the seeded current term (tests run one at a time).</summary>
public sealed class EnrollmentApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private async Task<HttpClient> StudentAsync(TermWindowState window = TermWindowState.Enrollment)
    {
        await factory.ResetStudentAsync();
        await factory.SetWindowAsync(window);
        return await factory.ClientForAsync(SisApiFactory.StudentId);
    }

    private static async Task<EnrollResultDto> EnrollAsync(HttpClient client, params int[] sectionIds)
    {
        var res = await client.PostAsJsonAsync("/api/enrollment/enroll", new EnrollRequest(sectionIds));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.ReadAsync<EnrollResultDto>();
    }

    [Fact]
    [Trait("Rule", "E9")]
    public async Task Enroll_FromCart_EnrollsRemovesFromCartAndLogsAction()
    {
        var client = await StudentAsync();
        var csc101 = await factory.SectionIdAsync("CSC 101");
        await client.PostAsJsonAsync("/api/cart", new AddToCartRequest(csc101));

        var result = await EnrollAsync(client, csc101);

        Assert.True(Assert.Single(result.Results).Success);
        var mine = await (await client.GetAsync("/api/enrollment/my")).ReadAsync<MyEnrollmentsDto>();
        Assert.Equal("CSC 101", Assert.Single(mine.Items).CourseCode);
        Assert.Equal(3, mine.EnrolledCredits);
        Assert.Empty((await (await client.GetAsync("/api/cart")).ReadAsync<CartDto>()).Items);
        await factory.WithDbAsync(async db =>
            Assert.True(await db.Set<EnrollmentAction>().AnyAsync(a => a.ToSectionId == csc101 && a.Action == EnrollmentActionType.Enroll)));
    }

    [Fact]
    [Trait("Rule", "E3")]
    public async Task Enroll_Several_ReportsEachOutcome()
    {
        var client = await StudentAsync();
        var csc101 = await factory.SectionIdAsync("CSC 101");
        var csc310 = await factory.SectionIdAsync("CSC 310");
        var mat201 = await factory.SectionIdAsync("MAT 201");

        var result = await EnrollAsync(client, csc101, csc310, mat201);

        Assert.Equal(2, result.EnrolledCount);
        var failed = Assert.Single(result.Results, r => !r.Success);
        Assert.Equal(csc310, failed.SectionId);
        Assert.Equal("CSC 310 A", failed.Label);
        Assert.Equal(EnrollmentPolicy.PrerequisiteMissing, failed.Code);
        Assert.Contains("CSC 210", failed.Message);
    }

    [Fact]
    [Trait("Rule", "E3")]
    public async Task Enroll_PrerequisitePassedWithD_Succeeds_ButFailedAttemptDoesNot()
    {
        var client = await StudentAsync();
        var csc210 = await factory.SectionIdAsync("CSC 210");
        await factory.AddCompletedAsync(SisApiFactory.StudentId, "CSC 101", 0m, "F");
        Assert.False((await EnrollAsync(client, csc210)).Results[0].Success);

        await factory.AddCompletedAsync(SisApiFactory.StudentId, "CSC 101", 1.0m, "D");
        Assert.True((await EnrollAsync(client, csc210)).Results[0].Success);
    }

    [Fact]
    [Trait("Rule", "E1")]
    public async Task Enroll_WindowClosed_Fails()
    {
        var client = await StudentAsync(TermWindowState.Closed);
        var outcome = (await EnrollAsync(client, await factory.SectionIdAsync("CSC 101"))).Results[0];
        Assert.Equal(EnrollmentPolicy.WindowClosed, outcome.Code);
    }

    [Fact]
    [Trait("Rule", "E4")]
    public async Task Enroll_SecondSectionOfSameCourse_Fails()
    {
        var client = await StudentAsync();
        await factory.AddCompletedAsync(SisApiFactory.StudentId, "CSC 101", 3.0m, "B");
        var result = await EnrollAsync(client, await factory.SectionIdAsync("CSC 210", "A"), await factory.SectionIdAsync("CSC 210", "B"));
        Assert.Equal(EnrollmentPolicy.SameCourse, result.Results[1].Code);
    }

    [Fact]
    [Trait("Rule", "E6")]
    public async Task Enroll_FullSection_Fails()
    {
        var client = await StudentAsync();
        var mat201 = await factory.SectionIdAsync("MAT 201");
        var other = await factory.ClientForAsync(await factory.AddStudentAsync(3902));
        await factory.ResetStudentAsync(3902);
        // Exactly one free seat, taken by the other student.
        await factory.WithDbAsync(async db =>
        {
            var taken = await db.Set<Enrollment>().CountAsync(e => e.SectionId == mat201 && e.Status == EnrollmentStatus.Enrolled);
            (await db.Set<Section>().SingleAsync(s => s.Id == mat201)).Capacity = taken + 1;
            await db.SaveChangesAsync();
        });
        try
        {
            Assert.True((await EnrollAsync(other, mat201)).Results[0].Success);

            var outcome = (await EnrollAsync(client, mat201)).Results[0];

            Assert.Equal(EnrollmentPolicy.SectionFull, outcome.Code);
            Assert.Equal("Section MAT 201 A is full.", outcome.Message);
        }
        finally
        {
            await factory.ResetStudentAsync(3902);
            await factory.WithDbAsync(async db =>
            {
                (await db.Set<Section>().SingleAsync(s => s.Id == mat201)).Capacity = 40;
                await db.SaveChangesAsync();
            });
        }
    }

    [Fact]
    public async Task Enroll_UnknownSection_ReportsNotFound_AndDuplicateIds_Return400()
    {
        var client = await StudentAsync();
        Assert.Equal("not_found", (await EnrollAsync(client, 99999)).Results[0].Code);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/enrollment/enroll", new EnrollRequest([1, 1]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/enrollment/enroll", new EnrollRequest([]))).StatusCode);
    }

    [Fact]
    [Trait("TC", "TC_63")]
    public async Task Swap_ToOtherSectionOfSameCourse_MovesTheEnrollment()
    {
        var client = await StudentAsync();
        await factory.AddCompletedAsync(SisApiFactory.StudentId, "CSC 101", 3.0m, "B");
        var a = await factory.SectionIdAsync("CSC 210", "A");
        var b = await factory.SectionIdAsync("CSC 210", "B");
        await EnrollAsync(client, a);

        var res = await client.PostAsJsonAsync("/api/enrollment/swap", new SwapRequest(a, b));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("B", Assert.Single((await res.ReadAsync<MyEnrollmentsDto>()).Items).SectionCode);
        await factory.WithDbAsync(async db =>
            Assert.True(await db.Set<EnrollmentAction>().AnyAsync(x => x.Action == EnrollmentActionType.Swap && x.FromSectionId == a && x.ToSectionId == b)));
    }

    [Fact]
    public async Task Swap_NotEnrolledInFrom_Returns409()
    {
        var client = await StudentAsync();
        var res = await client.PostAsJsonAsync("/api/enrollment/swap",
            new SwapRequest(await factory.SectionIdAsync("CSC 101"), await factory.SectionIdAsync("MAT 201")));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    [Trait("Rule", "E8")]
    public async Task Drop_InAddDrop_FreesTheSeat()
    {
        var client = await StudentAsync(TermWindowState.AddDrop);
        var mat201 = await factory.SectionIdAsync("MAT 201");
        await EnrollAsync(client, mat201);

        var res = await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(mat201));

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Empty((await (await client.GetAsync("/api/enrollment/my")).ReadAsync<MyEnrollmentsDto>()).Items);
        await factory.WithDbAsync(async db =>
        {
            var e = await db.Set<Enrollment>().SingleAsync(x => x.SectionId == mat201 && x.Student.User.UniversityId == SisApiFactory.StudentId);
            Assert.Equal(EnrollmentStatus.Dropped, e.Status);
            Assert.False(e.CountsInGpa);
            Assert.True(await db.Set<EnrollmentAction>().AnyAsync(x => x.Action == EnrollmentActionType.Drop && x.FromSectionId == mat201));
        });
    }

    [Fact]
    [Trait("Rule", "E1")]
    public async Task Drop_InEnrollmentWindow_Returns409()
    {
        var client = await StudentAsync(TermWindowState.Enrollment);
        var mat201 = await factory.SectionIdAsync("MAT 201");
        await EnrollAsync(client, mat201);

        var res = await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(mat201));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Contains(EnrollmentPolicy.DropWindowClosed, await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Drop_NotEnrolled_Returns404()
    {
        var client = await StudentAsync(TermWindowState.AddDrop);
        var res = await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(await factory.SectionIdAsync("MAT 201")));
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
