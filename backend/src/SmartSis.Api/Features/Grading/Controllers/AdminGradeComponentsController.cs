using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Grading.Controllers;

/// <summary>Admin: grade components per course per term — name, max grade and weight (PB-08, rule G1).</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/grade-components")]
[Tags("Admin: Grade components")]
public sealed class AdminGradeComponentsController(IGradeComponentService components) : ControllerBase
{
    /// <summary>Components of one course in one term, with their total weight.</summary>
    /// <response code="400">Course or term missing or unknown.</response>
    [HttpGet]
    [ProducesResponseType<GradeComponentListDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<GradeComponentListDto> List([FromQuery] GradeComponentQuery query, CancellationToken ct) => components.ListAsync(query, ct);

    /// <summary>One component.</summary>
    /// <response code="404">No component with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<GradeComponentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<GradeComponentDto> Get(int id, CancellationToken ct) => components.GetAsync(id, ct);

    /// <summary>Add a component to a course in a term.</summary>
    /// <response code="201">Created.</response>
    /// <response code="400">Invalid fields, or a course / term that doesn't exist.</response>
    /// <response code="409"><c>weight_over_100</c>, <c>duplicate_component_name</c> or <c>grades_finalized</c>.</response>
    [HttpPost]
    [ProducesResponseType<GradeComponentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GradeComponentDto>> Create(CreateGradeComponentRequest request, CancellationToken ct)
    {
        var component = await components.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = component.Id }, component);
    }

    /// <summary>Change a component's name, max grade or weight.</summary>
    /// <response code="400">Invalid fields.</response>
    /// <response code="404">No component with this id.</response>
    /// <response code="409"><c>weight_over_100</c>, <c>duplicate_component_name</c>, <c>scores_above_max</c> or <c>grades_finalized</c>.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<GradeComponentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<GradeComponentDto> Update(int id, UpdateGradeComponentRequest request, CancellationToken ct) =>
        components.UpdateAsync(id, request, ct);

    /// <summary>Delete a component that has no scores yet.</summary>
    /// <response code="404">No component with this id.</response>
    /// <response code="409"><c>component_has_scores</c> or <c>grades_finalized</c>.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await components.DeleteAsync(id, ct);
        return NoContent();
    }
}
