using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Sections.Interfaces;

/// <summary>Admin management of sections and their weekly meetings (PB-02, SR-21, UC-16).</summary>
public interface ISectionAdminService
{
    Task<PagedResult<SectionDto>> SearchAsync(SectionSearchQuery query, CancellationToken ct);
    Task<SectionDto> GetAsync(int id, CancellationToken ct);
    Task<SectionDto> CreateAsync(SaveSectionRequest request, CancellationToken ct);
    Task<SectionDto> UpdateAsync(int id, SaveSectionRequest request, CancellationToken ct);
}
