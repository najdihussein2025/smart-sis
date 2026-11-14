using SmartSis.Api.Features.Finance.Interfaces;
using SmartSis.Api.Features.Finance.Repositories;
using SmartSis.Api.Features.Finance.Services;

namespace SmartSis.Api.Features.Finance;

public static class FinanceModule
{
    public static IServiceCollection AddFinanceFeature(this IServiceCollection services)
    {
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<IFinanceAdminService, FinanceAdminService>();
        services.AddScoped<IMyFinanceService, MyFinanceService>();
        return services;
    }
}
