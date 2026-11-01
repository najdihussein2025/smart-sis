using SmartSis.Api.Features.Terms.Models.Dtos;

namespace SmartSis.Api.Features.Terms.Interfaces;

/// <summary>Admin management of terms and their registration windows (PB-02, SR-21).</summary>
public interface ITermAdminService
{
    Task<IReadOnlyList<TermDto>> ListAsync(CancellationToken ct);
    Task<TermDto> GetAsync(int id, CancellationToken ct);
    Task<TermDto> CreateAsync(SaveTermRequest request, CancellationToken ct);
    Task<TermDto> UpdateAsync(int id, SaveTermRequest request, CancellationToken ct);
}
