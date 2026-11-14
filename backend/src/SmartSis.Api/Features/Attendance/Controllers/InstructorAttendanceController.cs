using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Attendance.Interfaces;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Attendance.Controllers;

/// <summary>Instructor: take attendance for the sections they teach (PB-10).</summary>
[ApiController]
[Authorize(Policy = Policies.Instructor)]
[Route("api/attendance")]
[Tags("Instructor: Attendance")]
public sealed class InstructorAttendanceController(IAttendanceService attendance) : ControllerBase
{
    /// <summary>Sections the instructor teaches this term, with their weekly meetings and the term's dates.</summary>
    [HttpGet("teaching")]
    public Task<TeachingSectionsDto> Teaching(CancellationToken ct) => attendance.GetTeachingSectionsAsync(ct);

    /// <summary>
    /// The sheet for a meeting on a date: the enrolled students to mark, or, if attendance was already taken, what was
    /// recorded (<c>sessionId</c> is then set).
    /// </summary>
    /// <response code="403">The section isn't taught by this instructor.</response>
    /// <response code="404">The meeting doesn't exist.</response>
    /// <response code="409">The date is in the future, outside the term or not on the meeting's weekday.</response>
    [HttpGet("sheet")]
    [ProducesResponseType<AttendanceSheetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<AttendanceSheetDto> Sheet([FromQuery] int meetingId, [FromQuery] DateOnly date, CancellationToken ct) =>
        attendance.GetSheetAsync(meetingId, date, ct);

    /// <summary>Submitted sessions of a section, newest first.</summary>
    /// <response code="403">The section isn't taught by this instructor.</response>
    /// <response code="404">The section doesn't exist.</response>
    [HttpGet("sections/{sectionId:int}/sessions")]
    [ProducesResponseType<IReadOnlyList<SessionSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<SessionSummaryDto>> Sessions(int sectionId, CancellationToken ct) => attendance.ListSessionsAsync(sectionId, ct);

    /// <summary>A submitted session's records.</summary>
    /// <response code="403">The section isn't taught by this instructor.</response>
    /// <response code="404">The session doesn't exist.</response>
    [HttpGet("sessions/{sessionId:int}")]
    [ProducesResponseType<AttendanceSheetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AttendanceSheetDto> Session(int sessionId, CancellationToken ct) => attendance.GetSessionAsync(sessionId, ct);

    /// <summary>
    /// Submit Present / Absent for every enrolled student of a meeting on a date. Applies the absence rules: warning at
    /// limit − 1, limit alert at the limit (each email + notification once), auto-drop at limit + 1.
    /// </summary>
    /// <response code="201">Saved; the result lists the alerts it caused.</response>
    /// <response code="400">No students, a student twice, or a bad status.</response>
    /// <response code="403">The section isn't taught by this instructor.</response>
    /// <response code="404">The meeting doesn't exist.</response>
    /// <response code="409">Already taken for this meeting and date (<c>attendance_already_taken</c>), a bad date, or the marks don't match the roster.</response>
    [HttpPost("sessions")]
    [ProducesResponseType<SubmitAttendanceResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(SubmitAttendanceRequest request, CancellationToken ct)
    {
        var result = await attendance.SubmitAsync(request, ct);
        return CreatedAtAction(nameof(Session), new { sessionId = result.SessionId }, result);
    }
}
