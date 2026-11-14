using SmartSis.Api.Features.Schedule.Interfaces;
using SmartSis.Api.Features.Schedule.Repositories;
using SmartSis.Api.Features.Schedule.Services;

namespace SmartSis.Api.Features.Schedule;

public static class ScheduleModule
{
    public static IServiceCollection AddScheduleFeature(this IServiceCollection services)
    {
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IScheduleService, ScheduleService>();
        return services;
    }
}
