using SmartSis.Api.Features.Attendance.Interfaces;
using SmartSis.Api.Features.Attendance.Repositories;
using SmartSis.Api.Features.Attendance.Services;

namespace SmartSis.Api.Features.Attendance;

public static class AttendanceModule
{
    public static IServiceCollection AddAttendanceFeature(this IServiceCollection services)
    {
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        return services;
    }
}
