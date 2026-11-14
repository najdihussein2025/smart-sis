using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Repositories;
using SmartSis.Api.Features.Support.Services;

namespace SmartSis.Api.Features.Support;

public static class SupportModule
{
    public static IServiceCollection AddSupportFeature(this IServiceCollection services)
    {
        services.AddScoped<ISupportRepository, SupportRepository>();
        services.AddScoped<SupportNotifier>();
        services.AddScoped<ISupportService, SupportService>();
        return services;
    }
}
