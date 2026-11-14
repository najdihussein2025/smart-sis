using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Enrollments.Interfaces;
using SmartSis.Api.Features.Enrollments.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Enrollments.Controllers;

/// <summary>Student: enroll, swap and drop (PB-05, PB-07).</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/enrollment")]
[Tags("Student: Enrollment")]
public sealed class EnrollmentController(IEnrollmentService enrollment) : ControllerBase
{
    /// <summary>Sections the student is enrolled in this term, with credits, cap and the open window.</summary>
    [HttpGet("my")]
    public Task<MyEnrollmentsDto> Mine(CancellationToken ct) => enrollment.GetMineAsync(ct);

    /// <summary>
    /// Enroll in one or more sections. Each one is checked in order (window → credit cap → prerequisites → same course →
    /// time conflict → capacity) and saved on its own; the result says which succeeded and why each other one failed.
    /// </summary>
    /// <response code="200">Per-section results (some may have failed).</response>
    /// <response code="400">No section ids, more than 10, or duplicates.</response>
    [HttpPost("enroll")]
    [ProducesResponseType<EnrollResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<EnrollResultDto> Enroll(EnrollRequest request, CancellationToken ct) => enrollment.EnrollAsync(request, ct);

    /// <summary>Swap an enrolled section for another in one step (enrollment or add/drop period).</summary>
    /// <response code="404">The section to swap to doesn't exist.</response>
    /// <response code="409">A rule failed; <c>code</c> says which.</response>
    [HttpPost("swap")]
    [ProducesResponseType<MyEnrollmentsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<MyEnrollmentsDto> Swap(SwapRequest request, CancellationToken ct) => enrollment.SwapAsync(request, ct);

    /// <summary>Drop an enrolled section. Only during the add/drop period; the seat returns to the section.</summary>
    /// <response code="404">The student isn't enrolled in this section.</response>
    /// <response code="409"><c>drop_window_closed</c>.</response>
    [HttpPost("drop")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Drop(DropRequest request, CancellationToken ct)
    {
        await enrollment.DropAsync(request, ct);
        return NoContent();
    }
}
