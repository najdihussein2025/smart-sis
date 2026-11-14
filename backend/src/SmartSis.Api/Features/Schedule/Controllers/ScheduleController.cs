using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Schedule.Interfaces;
using SmartSis.Api.Features.Schedule.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Schedule.Controllers;

/// <summary>Weekly timetables for the current term (PB-06). Each action declares its role.</summary>
[ApiController]
[Route("api/schedule")]
[Tags("Schedule")]
public sealed class ScheduleController(IScheduleService schedule) : ControllerBase
{
    /// <summary>Student: their classes this term (enrolled sections), by day and time.</summary>
    [HttpGet("my")]
    [Authorize(Policy = Policies.Student)]
    public Task<ScheduleDto> Mine(CancellationToken ct) => schedule.GetStudentScheduleAsync(ct);

    /// <summary>Instructor: the sections they teach this term, by day and time, with lecture / lab labels.</summary>
    [HttpGet("teaching")]
    [Authorize(Policy = Policies.Instructor)]
    public Task<ScheduleDto> Teaching(CancellationToken ct) => schedule.GetTeachingScheduleAsync(ct);
}
