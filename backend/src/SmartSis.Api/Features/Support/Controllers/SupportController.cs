using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Controllers;

/// <summary>Student: submit support requests, follow their status and reply in the thread (PB-13).</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/support")]
[Tags("Student: Support")]
public sealed class SupportController(ISupportService support) : ControllerBase
{
    /// <summary>Categories to choose from, by name.</summary>
    [HttpGet("categories")]
    public Task<List<SupportCategoryDto>> Categories(CancellationToken ct) => support.ListCategoriesAsync(ct);

    /// <summary>My requests, newest activity first. Filter by status.</summary>
    [HttpGet("requests")]
    public Task<PagedResult<SupportRequestSummaryDto>> List([FromQuery] MySupportRequestsQuery query, CancellationToken ct) =>
        support.ListMineAsync(query, ct);

    /// <summary>One of my requests with its thread.</summary>
    /// <response code="404">No request with this id among mine.</response>
    [HttpGet("requests/{id:int}")]
    [ProducesResponseType<SupportRequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<SupportRequestDetailDto> Get(int id, CancellationToken ct) => support.GetMineAsync(id, ct);

    /// <summary>Submit a request. It starts Open and unassigned.</summary>
    /// <response code="201">Created; <c>Location</c> points to the request.</response>
    /// <response code="400">Invalid fields, or a category that doesn't exist.</response>
    [HttpPost("requests")]
    [ProducesResponseType<SupportRequestDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupportRequestDetailDto>> Create(CreateSupportRequestRequest request, CancellationToken ct)
    {
        var created = await support.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Reply in the thread. A reply on a Resolved request reopens it.</summary>
    /// <response code="400">Empty or too long.</response>
    /// <response code="404">No request with this id among mine.</response>
    /// <response code="409"><c>request_closed</c>: a closed request takes no replies.</response>
    [HttpPost("requests/{id:int}/messages")]
    [ProducesResponseType<SupportRequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<SupportRequestDetailDto> Reply(int id, SupportReplyRequest request, CancellationToken ct) =>
        support.ReplyAsync(id, request, ct);
}
