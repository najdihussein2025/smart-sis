using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Features.Schedule.Models.Dtos;
using SmartSis.Api.Features.Sections.Enums;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Schedule;

/// <summary>PB-06 / T23: weekly schedules from the seeded current term.</summary>
public sealed class ScheduleApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private async Task<HttpClient> StudentEnrolledInAsync(params (string Course, string Section)[] sections)
    {
        await factory.ResetStudentAsync();
        await factory.SetWindowAsync(TermWindowState.Enrollment);
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        var ids = new List<int>();
        foreach (var (course, section) in sections) ids.Add(await factory.SectionIdAsync(course, section));
        if (ids.Count > 0) await client.PostAsJsonAsync("/api/enrollment/enroll", new EnrollRequest([.. ids]));
        return client;
    }

    [Fact]
    public async Task Student_SeesOnlyEnrolledSections_ByDayAndTime()
    {
        var client = await StudentEnrolledInAsync(("MAT 201", "A"), ("CSC 101", "A"));

        var schedule = await (await client.GetAsync("/api/schedule/my")).ReadAsync<ScheduleDto>();

        // CSC 101 A: Mon + Wed 08:00. MAT 201 A: Mon + Wed 10:00.
        Assert.Equal(4, schedule.Items.Count);
        Assert.Equal(["CSC 101", "MAT 201", "CSC 101", "MAT 201"], schedule.Items.Select(i => i.CourseCode));
        Assert.Equal([1, 1, 3, 3], schedule.Items.Select(i => i.DayOfWeek));
        var first = schedule.Items[0];
        Assert.Equal(new TimeOnly(8, 0), first.StartTime);
        Assert.Equal("B-101", first.RoomName);
        Assert.Equal("Demo Instructor", first.InstructorName);
    }

    [Fact]
    public async Task Student_NothingEnrolled_EmptySchedule()
    {
        var client = await StudentEnrolledInAsync();
        var schedule = await (await client.GetAsync("/api/schedule/my")).ReadAsync<ScheduleDto>();
        Assert.NotNull(schedule.TermId);
        Assert.Empty(schedule.Items);
    }

    [Fact]
    public async Task Student_DroppedSection_LeavesTheSchedule()
    {
        var client = await StudentEnrolledInAsync(("MAT 201", "A"));
        await factory.SetWindowAsync(TermWindowState.AddDrop);
        await client.PostAsJsonAsync("/api/enrollment/drop", new DropRequest(await factory.SectionIdAsync("MAT 201")));

        Assert.Empty((await (await client.GetAsync("/api/schedule/my")).ReadAsync<ScheduleDto>()).Items);
    }

    [Fact]
    public async Task Instructor_SeesTaughtSections_WithLabLabels()
    {
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        var schedule = await (await client.GetAsync("/api/schedule/teaching")).ReadAsync<ScheduleDto>();

        // The seeded instructor teaches all five seeded sections: 10 weekly meetings, two of them labs.
        Assert.Equal(10, schedule.Items.Count);
        Assert.Equal(2, schedule.Items.Count(i => i.Type == MeetingType.Lab));
        Assert.Equal(schedule.Items.OrderBy(i => i.DayOfWeek).ThenBy(i => i.StartTime).Select(i => i.MeetingId), schedule.Items.Select(i => i.MeetingId));
    }

    [Fact]
    public async Task NoCurrentTerm_EmptySchedule()
    {
        var client = await factory.ClientForAsync(SisApiFactory.InstructorId);
        await factory.WithDbAsync(async db =>
        {
            (await db.Set<Term>().SingleAsync(t => t.IsCurrent)).IsCurrent = false;
            await db.SaveChangesAsync();
        });
        try
        {
            var schedule = await (await client.GetAsync("/api/schedule/teaching")).ReadAsync<ScheduleDto>();
            Assert.Null(schedule.TermId);
            Assert.Empty(schedule.Items);
        }
        finally
        {
            await factory.WithDbAsync(async db =>
            {
                (await db.Set<Term>().OrderBy(t => t.Id).FirstAsync()).IsCurrent = true;
                await db.SaveChangesAsync();
            });
        }
    }
}
