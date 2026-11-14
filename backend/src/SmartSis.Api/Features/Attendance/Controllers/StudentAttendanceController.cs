using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Attendance.Interfaces;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Attendance.Controllers;

/// <summary>Student: their own attendance (PB-11). The AI assistant calls this with the student's token.</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/attendance")]
[Tags("Student: Attendance")]
public sealed class StudentAttendanceController(IAttendanceService attendance) : ControllerBase
{
    /// <summary>
    /// Absences per course this term: count, limit (credits × 3), status colour (Perfect / Warning / Critical) and the
    /// date of each absence. A course dropped for absences stays listed with status <c>DroppedAbsence</c>.
    /// </summary>
    [HttpGet("my")]
    public Task<MyAttendanceDto> Mine(CancellationToken ct) => attendance.GetMineAsync(ct);
}
