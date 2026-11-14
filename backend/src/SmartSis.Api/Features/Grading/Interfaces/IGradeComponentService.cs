using SmartSis.Api.Features.Grading.Models.Dtos;

namespace SmartSis.Api.Features.Grading.Interfaces;

/// <summary>Admin: grade components per course per term (PB-08, rule G1).</summary>
public interface IGradeComponentService
{
    Task<GradeComponentListDto> ListAsync(GradeComponentQuery query, CancellationToken ct);
    Task<GradeComponentDto> GetAsync(int id, CancellationToken ct);
    Task<GradeComponentDto> CreateAsync(CreateGradeComponentRequest request, CancellationToken ct);
    Task<GradeComponentDto> UpdateAsync(int id, UpdateGradeComponentRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
