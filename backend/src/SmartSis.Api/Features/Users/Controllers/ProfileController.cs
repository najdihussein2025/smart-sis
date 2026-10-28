using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Users.Controllers;

/// <summary>Profile of the signed-in user (any role).</summary>
[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IUserAccountService accounts, ICurrentUser me) : ControllerBase
{
    [HttpGet]
    public Task<UserProfileDto> Get(CancellationToken ct) => accounts.GetProfileAsync(me.UserId, ct);
}
