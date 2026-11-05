using SmartSis.Api.Features.Lookups.Interfaces;
using SmartSis.Api.Features.Lookups.Models.Dtos;

namespace SmartSis.Api.Features.Lookups.Services;

public sealed class LookupService(ILookupRepository lookups) : ILookupService
{
    // One DbContext per request, so the queries run one after the other (EF doesn't allow parallel queries on one context).
    public async Task<LookupsDto> GetAllAsync(CancellationToken ct) => new(
        await lookups.CampusesAsync(ct),
        await lookups.DepartmentsAsync(ct),
        await lookups.MajorsAsync(ct),
        await lookups.RoomsAsync(ct),
        await lookups.ActiveInstructorsAsync(ct));
}
