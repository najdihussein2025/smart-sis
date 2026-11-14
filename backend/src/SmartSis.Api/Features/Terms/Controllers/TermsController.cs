using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Terms.Interfaces;
using SmartSis.Api.Features.Terms.Models.Dtos;

namespace SmartSis.Api.Features.Terms.Controllers;

/// <summary>Any signed-in user: the current term and its registration windows (read-only).</summary>
[ApiController]
[Authorize]
[Route("api/terms")]
[Tags("Terms")]
public sealed class TermsController(ITermQueryService terms) : ControllerBase
{
    /// <summary>The current term with its enrollment and add/drop windows (UTC) and the window open now.</summary>
    /// <response code="404">No term is current.</response>
    [HttpGet("current")]
    [ProducesResponseType<TermDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<TermDto> Current(CancellationToken ct) => terms.GetCurrentAsync(ct);
}
