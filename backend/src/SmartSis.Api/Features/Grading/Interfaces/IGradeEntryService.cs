using SmartSis.Api.Features.Grading.Models.Dtos;

namespace SmartSis.Api.Features.Grading.Interfaces;

/// <summary>Instructor: enter scores and finalize grades for sections they teach (PB-08, rule G2).</summary>
public interface IGradeEntryService
{
    Task<IReadOnlyList<TeachingSectionDto>> ListSectionsAsync(CancellationToken ct);
    Task<GradingSectionDto> GetSectionAsync(int sectionId, CancellationToken ct);
    Task<ComponentRosterDto> GetRosterAsync(int sectionId, int componentId, CancellationToken ct);
    Task<ComponentRosterDto> SaveScoresAsync(int sectionId, int componentId, SaveScoresRequest request, CancellationToken ct);

    /// <summary>Completes every Enrolled student with their final grade (rules G3, G5) and rebuilds their TermResults (rule G6).</summary>
    Task<FinalizeResultDto> FinalizeAsync(int sectionId, CancellationToken ct);
}

/// <summary>Student: own grades this term and transcript (PB-09).</summary>
public interface IStudentGradesService
{
    Task<MyGradesDto> GetMineAsync(CancellationToken ct);
    Task<TranscriptDto> GetTranscriptAsync(CancellationToken ct);
}

/// <summary>Rule G6: TermResults is a cache rebuilt from the enrollments, never edited by hand.</summary>
public interface ITermResultService
{
    /// <summary>Recomputes every TermResults row of the student. Does not save: the caller's unit of work commits it.</summary>
    Task RebuildAsync(int studentId, CancellationToken ct);
}
