using SmartSis.Api.Features.Schedule.Interfaces;
using SmartSis.Api.Features.Schedule.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Schedule.Services;

public sealed class ScheduleService(IScheduleRepository schedule, ICurrentUser me) : IScheduleService
{
    public async Task<ScheduleDto> GetStudentScheduleAsync(CancellationToken ct)
    {
        var term = await schedule.GetCurrentTermAsync(ct);
        if (term is null) return new ScheduleDto(null, null, []);
        return new ScheduleDto(term.Value.Id, term.Value.Name, await schedule.GetStudentItemsAsync(me.UserId, term.Value.Id, ct));
    }

    public async Task<ScheduleDto> GetTeachingScheduleAsync(CancellationToken ct)
    {
        var term = await schedule.GetCurrentTermAsync(ct);
        if (term is null) return new ScheduleDto(null, null, []);
        return new ScheduleDto(term.Value.Id, term.Value.Name, await schedule.GetInstructorItemsAsync(me.UserId, term.Value.Id, ct));
    }
}
