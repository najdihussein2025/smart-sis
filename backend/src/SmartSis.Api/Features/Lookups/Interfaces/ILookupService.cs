using SmartSis.Api.Features.Lookups.Models.Dtos;

namespace SmartSis.Api.Features.Lookups.Interfaces;

/// <summary>Read-only reference lists for admin forms (PB-02, PB-03).</summary>
public interface ILookupService
{
    Task<LookupsDto> GetAllAsync(CancellationToken ct);
}
