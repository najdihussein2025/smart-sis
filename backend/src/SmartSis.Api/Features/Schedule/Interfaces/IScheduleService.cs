using SmartSis.Api.Features.Schedule.Models.Dtos;

namespace SmartSis.Api.Features.Schedule.Interfaces;

/// <summary>Read-only weekly timetables (PB-06). The user comes from the token.</summary>
public interface IScheduleService
{
    /// <summary>Meetings of the sections the student is Enrolled in this term.</summary>
    Task<ScheduleDto> GetStudentScheduleAsync(CancellationToken ct);

    /// <summary>Meetings of the sections the instructor teaches this term.</summary>
    Task<ScheduleDto> GetTeachingScheduleAsync(CancellationToken ct);
}

public interface IScheduleRepository
{
    Task<(int Id, string Name)?> GetCurrentTermAsync(CancellationToken ct);
    Task<List<ScheduleItemDto>> GetStudentItemsAsync(int studentId, int termId, CancellationToken ct);
    Task<List<ScheduleItemDto>> GetInstructorItemsAsync(int instructorId, int termId, CancellationToken ct);
}
