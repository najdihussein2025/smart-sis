using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Grading.Controllers;

/// <summary>Instructor: section → component → score per student, then finalize (PB-08). Only sections they teach.</summary>
[ApiController]
[Authorize(Policy = Policies.Instructor)]
[Route("api/instructor/grading")]
[Tags("Instructor: Grading")]
public sealed class InstructorGradingController(IGradeEntryService grading) : ControllerBase
{
    /// <summary>Current-term sections the instructor teaches, with how many scores are still missing.</summary>
    [HttpGet("sections")]
    public Task<IReadOnlyList<TeachingSectionDto>> Sections(CancellationToken ct) => grading.ListSectionsAsync(ct);

    /// <summary>A section's components, each with its "not graded" count.</summary>
    /// <response code="403">The instructor doesn't teach this section.</response>
    /// <response code="404">No section with this id.</response>
    [HttpGet("sections/{sectionId:int}")]
    [ProducesResponseType<GradingSectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<GradingSectionDto> Section(int sectionId, CancellationToken ct) => grading.GetSectionAsync(sectionId, ct);

    /// <summary>The roster for one component with existing scores filled in.</summary>
    /// <response code="403">The instructor doesn't teach this section.</response>
    /// <response code="404">No such section, or the component isn't one of its course's components this term.</response>
    [HttpGet("sections/{sectionId:int}/components/{componentId:int}")]
    [ProducesResponseType<ComponentRosterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ComponentRosterDto> Roster(int sectionId, int componentId, CancellationToken ct) =>
        grading.GetRosterAsync(sectionId, componentId, ct);

    /// <summary>Save scores for one component (0 … max grade; null clears a score). Students left out keep their score.</summary>
    /// <response code="400">A score out of range, or a student who isn't enrolled in the section (field errors in <c>errors</c>).</response>
    /// <response code="403">The instructor doesn't teach this section.</response>
    /// <response code="404">No such section or component.</response>
    /// <response code="409"><c>grades_finalized</c>.</response>
    [HttpPut("sections/{sectionId:int}/components/{componentId:int}")]
    [ProducesResponseType<ComponentRosterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ComponentRosterDto> SaveScores(int sectionId, int componentId, SaveScoresRequest request, CancellationToken ct) =>
        grading.SaveScoresAsync(sectionId, componentId, request, ct);

    /// <summary>
    /// Finalize the section: every enrolled student gets their final %, letter and grade points, older attempts of the course stop
    /// counting in the GPA, and term results are rebuilt. Needs components weighing 100% and every score entered.
    /// </summary>
    /// <response code="403">The instructor doesn't teach this section.</response>
    /// <response code="404">No section with this id.</response>
    /// <response code="409"><c>already_finalized</c>, <c>no_students</c>, <c>no_components</c>, <c>weights_incomplete</c> or <c>scores_missing</c>.</response>
    [HttpPost("sections/{sectionId:int}/finalize")]
    [ProducesResponseType<FinalizeResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<FinalizeResultDto> Finalize(int sectionId, CancellationToken ct) => grading.FinalizeAsync(sectionId, ct);
}
