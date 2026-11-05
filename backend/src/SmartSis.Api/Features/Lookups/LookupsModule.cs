using SmartSis.Api.Features.Lookups.Interfaces;
using SmartSis.Api.Features.Lookups.Repositories;
using SmartSis.Api.Features.Lookups.Services;

namespace SmartSis.Api.Features.Lookups;

public static class LookupsModule
{
    public static IServiceCollection AddLookupsFeature(this IServiceCollection services)
    {
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<ILookupService, LookupService>();
        // TODO(phase-2): admin create / edit for campuses, departments, majors and rooms.
        return services;
    }
}
