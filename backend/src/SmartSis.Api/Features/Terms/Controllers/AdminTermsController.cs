using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Terms.Interfaces;
using SmartSis.Api.Features.Terms.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Terms.Controllers;

/// <summary>Admin: create and edit terms and their enrollment / add-drop windows.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/terms")]
[Tags("Admin: Terms")]
public sealed class AdminTermsController(ITermAdminService terms) : ControllerBase
{
    /// <summary>List all terms, newest start date first.</summary>
    [HttpGet]
    public Task<IReadOnlyList<TermDto>> List(CancellationToken ct) => terms.ListAsync(ct);

    /// <summary>Get one term.</summary>
    /// <response code="404">No term with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TermDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TermDto> Get(int id, CancellationToken ct) => terms.GetAsync(id, ct);

    /// <summary>Create a term. Setting <c>isCurrent</c> unsets the previous current term.</summary>
    /// <response code="201">Created; <c>Location</c> points to the new term.</response>
    /// <response code="400">Invalid fields or dates (field errors in <c>errors</c>).</response>
    /// <response code="409">A term with this name already exists (<c>code: duplicate_term_name</c>).</response>
    [HttpPost]
    [ProducesResponseType<TermDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TermDto>> Create(SaveTermRequest request, CancellationToken ct)
    {
        var term = await terms.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = term.Id }, term);
    }

    /// <summary>Replace a term's fields. Setting <c>isCurrent</c> unsets the previous current term.</summary>
    /// <response code="400">Invalid fields or dates.</response>
    /// <response code="404">No term with this id.</response>
    /// <response code="409">Another term already has this name (<c>code: duplicate_term_name</c>).</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<TermDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<TermDto> Update(int id, SaveTermRequest request, CancellationToken ct) => terms.UpdateAsync(id, request, ct);
}
