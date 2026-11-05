using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Lookups.Interfaces;
using SmartSis.Api.Features.Lookups.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Lookups.Controllers;

/// <summary>Admin: reference lists for form dropdowns (campuses, departments, majors, rooms, instructors).</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/lookups")]
[Tags("Admin: Lookups")]
public sealed class AdminLookupsController(ILookupService lookups) : ControllerBase
{
    /// <summary>All lookup lists in one call. The tables are small; cache the result on the client.</summary>
    [HttpGet]
    public Task<LookupsDto> Get(CancellationToken ct) => lookups.GetAllAsync(ct);
}
