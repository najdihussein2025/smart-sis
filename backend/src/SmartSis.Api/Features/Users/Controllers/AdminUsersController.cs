using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Users.Controllers;

/// <summary>Admin: add, edit and deactivate student and instructor accounts.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/users")]
[Tags("Admin: Users")]
public sealed class AdminUsersController(IUserAdminService users) : ControllerBase
{
    /// <summary>Search accounts, ordered by full name. Default <c>status</c> is Active.</summary>
    [HttpGet]
    public Task<PagedResult<UserListItemDto>> List([FromQuery] UserSearchQuery query, CancellationToken ct) =>
        users.SearchAsync(query, ct);

    /// <summary>Get one account, including deactivated ones.</summary>
    /// <response code="404">No user with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<UserDetailDto> Get(int id, CancellationToken ct) => users.GetAsync(id, ct);

    /// <summary>Create a student or instructor with an initial password.</summary>
    /// <response code="201">Created; <c>Location</c> points to the new account.</response>
    /// <response code="400">Invalid fields, Admin role, profile block not matching the role, or a major / campus / department that doesn't exist.</response>
    /// <response code="409">University ID (<c>duplicate_university_id</c>) or email (<c>duplicate_email</c>) already used, including by a deactivated account.</response>
    [HttpPost]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDetailDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var user = await users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    /// <summary>Edit an account. University ID, role and password don't change.</summary>
    /// <response code="400">Invalid fields, profile block not matching the role, or a major / campus / department that doesn't exist.</response>
    /// <response code="404">No user with this id.</response>
    /// <response code="409">Email already used (<c>duplicate_email</c>), account deactivated (<c>user_inactive</c>) or an Admin account (<c>admin_not_managed</c>).</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<UserDetailDto> Update(int id, UpdateUserRequest request, CancellationToken ct) => users.UpdateAsync(id, request, ct);

    /// <summary>Deactivate an account (soft delete). The user can no longer sign in. Calling it again does nothing.</summary>
    /// <response code="204">Deactivated (or already was).</response>
    /// <response code="404">No user with this id.</response>
    /// <response code="409">Your own account (<c>cannot_deactivate_self</c>) or an Admin account (<c>admin_not_managed</c>).</response>
    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        await users.DeactivateAsync(id, ct);
        return NoContent();
    }
}
