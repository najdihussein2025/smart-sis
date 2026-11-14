using SmartSis.Api.Features.Dashboard.Interfaces;
using SmartSis.Api.Features.Dashboard.Repositories;
using SmartSis.Api.Features.Dashboard.Services;

namespace SmartSis.Api.Features.Dashboard;

public static class DashboardModule
{
    public static IServiceCollection AddDashboardFeature(this IServiceCollection services)
    {
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDashboardService, DashboardService>();
        return services;
    }
}
