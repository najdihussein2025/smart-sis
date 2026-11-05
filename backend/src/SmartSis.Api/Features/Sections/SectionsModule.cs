using SmartSis.Api.Features.Sections.Interfaces;
using SmartSis.Api.Features.Sections.Repositories;
using SmartSis.Api.Features.Sections.Services;

namespace SmartSis.Api.Features.Sections;

public static class SectionsModule
{
    public static IServiceCollection AddSectionsFeature(this IServiceCollection services)
    {
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<ISectionAdminService, SectionAdminService>();
        // TODO(phase-3): read-only section endpoints for the student catalog and instructor schedule.
        return services;
    }
}
