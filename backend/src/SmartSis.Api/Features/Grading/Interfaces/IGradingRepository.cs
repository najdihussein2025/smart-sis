using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Models.Entities;

namespace SmartSis.Api.Features.Grading.Interfaces;

/// <summary>EF queries for the Grading feature. No SaveChanges here.</summary>
public interface IGradingRepository
{
    // ---- Components (admin) ----
    Task<bool> CourseExistsAsync(int courseId, CancellationToken ct);
    Task<bool> TermExistsAsync(int termId, CancellationToken ct);
    Task<List<GradeComponentDto>> ListComponentsAsync(int courseId, int termId, CancellationToken ct);
    Task<GradeComponentDto?> GetComponentDtoAsync(int id, CancellationToken ct);

    /// <summary>Tracked.</summary>
    Task<GradeComponent?> FindComponentAsync(int id, CancellationToken ct);

    Task<List<decimal>> OtherWeightsAsync(int courseId, int termId, int? excludeId, CancellationToken ct);
    Task<bool> ComponentNameExistsAsync(int courseId, int termId, string name, int? excludeId, CancellationToken ct);
    Task<int> CountScoresAsync(int componentId, CancellationToken ct);
    Task<decimal?> HighestScoreAsync(int componentId, CancellationToken ct);

    /// <summary>True when a section of the course in the term already has completed (finalized) enrollments.</summary>
    Task<bool> IsFinalizedAsync(int courseId, int termId, CancellationToken ct);

    void Add(GradeComponent component);
    void Remove(GradeComponent component);
}
