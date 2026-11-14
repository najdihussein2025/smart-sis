using SmartSis.Api.Features.Dashboard.Models.Dtos;

namespace SmartSis.Api.Features.Dashboard.Interfaces;

/// <summary>Read-only numbers for the three role dashboards (PB-22). The user always comes from the token.</summary>
public interface IDashboardService
{
    Task<StudentDashboardDto> GetStudentAsync(CancellationToken ct);
    Task<InstructorDashboardDto> GetInstructorAsync(CancellationToken ct);
    Task<AdminDashboardDto> GetAdminAsync(CancellationToken ct);
}

/// <summary>Counts the owning features don't expose. Read-only queries across features, no SaveChanges.</summary>
public interface IDashboardRepository
{
    /// <summary>(MeetingId, SessionDate) of the attendance sessions taken for these meetings between the two dates (inclusive).</summary>
    Task<List<(int MeetingId, DateOnly Date)>> TakenSessionsAsync(IReadOnlyCollection<int> meetingIds, DateOnly from, DateOnly to, CancellationToken ct);

    Task<AdminDashboardDto> GetAdminAsync(CancellationToken ct);
}
