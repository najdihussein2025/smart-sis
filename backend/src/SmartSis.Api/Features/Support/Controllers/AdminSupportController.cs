using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Controllers;

/// <summary>Admin: the support inbox (PB-14). Every request is handled by admins (rule C3).</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/support")]
[Tags("Admin: Support")]
public sealed class AdminSupportController(IAdminSupportService support) : ControllerBase
{
    /// <summary>Search requests, newest activity first. Filter by status, category, assignee and text.</summary>
    [HttpGet]
    public Task<PagedResult<AdminSupportRequestDto>> List([FromQuery] AdminSupportSearchQuery query, CancellationToken ct) =>
        support.SearchAsync(query, ct);

    /// <summary>Active admins a request can be assigned to.</summary>
    [HttpGet("admins")]
    public Task<List<SupportAdminDto>> Admins(CancellationToken ct) => support.ListAdminsAsync(ct);

    /// <summary>One request with the student and the thread.</summary>
    /// <response code="404">No request with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AdminSupportRequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AdminSupportRequestDetailDto> Get(int id, CancellationToken ct) => support.GetAsync(id, ct);

    /// <summary>Change the status. Resolved / Closed set <c>resolvedAt</c>; Open / In progress clear it. The student is notified.</summary>
    /// <response code="400">Unknown status.</response>
    /// <response code="404">No request with this id.</response>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType<AdminSupportRequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AdminSupportRequestDetailDto> ChangeStatus(int id, ChangeSupportStatusRequest request, CancellationToken ct) =>
        support.ChangeStatusAsync(id, request, ct);

    /// <summary>Assign the request to an active admin, or unassign it with <c>adminId: null</c>.</summary>
    /// <response code="400">Not an active admin.</response>
    /// <response code="404">No request with this id.</response>
    [HttpPut("{id:int}/assignee")]
    [ProducesResponseType<AdminSupportRequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AdminSupportRequestDetailDto> Assign(int id, AssignSupportRequestRequest request, CancellationToken ct) =>
        support.AssignAsync(id, request, ct);

    /// <summary>Reply in the thread. An Open request moves to In progress. The student is notified and emailed.</summary>
    /// <response code="400">Empty or too long.</response>
    /// <response code="404">No request with this id.</response>
    /// <response code="409"><c>request_closed</c>: reopen the request first.</response>
    [HttpPost("{id:int}/messages")]
    [ProducesResponseType<AdminSupportRequestDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<AdminSupportRequestDetailDto> Reply(int id, SupportReplyRequest request, CancellationToken ct) =>
        support.ReplyAsync(id, request, ct);
}

/// <summary>Admin: support categories (list, add, rename, delete when unused).</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/support/categories")]
[Tags("Admin: Support")]
public sealed class AdminSupportCategoriesController(ISupportCategoryService categories) : ControllerBase
{
    /// <summary>All categories by name, with how many requests use each.</summary>
    [HttpGet]
    public Task<List<AdminSupportCategoryDto>> List(CancellationToken ct) => categories.ListAsync(ct);

    /// <summary>Add a category.</summary>
    /// <response code="400">Empty or too long.</response>
    /// <response code="409"><c>duplicate_category</c>.</response>
    [HttpPost]
    [ProducesResponseType<AdminSupportCategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminSupportCategoryDto>> Create(SaveSupportCategoryRequest request, CancellationToken ct)
    {
        var created = await categories.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Rename a category. Requests keep it.</summary>
    /// <response code="400">Empty or too long.</response>
    /// <response code="404">No category with this id.</response>
    /// <response code="409"><c>duplicate_category</c>.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<AdminSupportCategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<AdminSupportCategoryDto> Rename(int id, SaveSupportCategoryRequest request, CancellationToken ct) =>
        categories.RenameAsync(id, request, ct);

    /// <summary>Delete a category no request uses.</summary>
    /// <response code="404">No category with this id.</response>
    /// <response code="409"><c>category_in_use</c>: rename it instead.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await categories.DeleteAsync(id, ct);
        return NoContent();
    }
}
