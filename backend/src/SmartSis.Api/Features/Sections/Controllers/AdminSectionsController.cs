using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Sections.Interfaces;
using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Sections.Controllers;

/// <summary>Admin: create and edit sections — course, term, instructor, capacity and weekly meetings.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/sections")]
[Tags("Admin: Sections")]
public sealed class AdminSectionsController(ISectionAdminService sections) : ControllerBase
{
    /// <summary>Search sections, ordered by course code then section code. Filter by term and / or course.</summary>
    [HttpGet]
    public Task<PagedResult<SectionDto>> List([FromQuery] SectionSearchQuery query, CancellationToken ct) =>
        sections.SearchAsync(query, ct);

    /// <summary>Get one section with its meetings and seat count.</summary>
    /// <response code="404">No section with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<SectionDto> Get(int id, CancellationToken ct) => sections.GetAsync(id, ct);

    /// <summary>Create a section with its meetings. The section code is stored upper case.</summary>
    /// <response code="201">Created; <c>Location</c> points to the new section.</response>
    /// <response code="400">Invalid fields, or a course / term / instructor / room that doesn't exist (field errors in <c>errors</c>).</response>
    /// <response code="409">Code already used (<c>duplicate_section_code</c>), or a clash: <c>meeting_overlap</c>, <c>room_clash</c>, <c>instructor_clash</c>.</response>
    [HttpPost]
    [ProducesResponseType<SectionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SectionDto>> Create(SaveSectionRequest request, CancellationToken ct)
    {
        var section = await sections.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = section.Id }, section);
    }

    /// <summary>Replace a section's fields and meetings. Send a meeting's <c>id</c> to keep it; meetings left out are removed.</summary>
    /// <response code="400">Invalid fields, a reference that doesn't exist, or a meeting id from another section.</response>
    /// <response code="404">No section with this id.</response>
    /// <response code="409">
    /// <c>duplicate_section_code</c>, a clash (<c>meeting_overlap</c>, <c>room_clash</c>, <c>instructor_clash</c>),
    /// <c>capacity_below_taken</c>, <c>section_has_enrollments</c> (course / term change) or <c>meeting_has_attendance</c> (removing a used meeting).
    /// </response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<SectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<SectionDto> Update(int id, SaveSectionRequest request, CancellationToken ct) =>
        sections.UpdateAsync(id, request, ct);
}
