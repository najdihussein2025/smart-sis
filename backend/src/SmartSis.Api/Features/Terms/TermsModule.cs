using SmartSis.Api.Features.Terms.Interfaces;
using SmartSis.Api.Features.Terms.Repositories;
using SmartSis.Api.Features.Terms.Services;

namespace SmartSis.Api.Features.Terms;

public static class TermsModule
{
    public static IServiceCollection AddTermsFeature(this IServiceCollection services)
    {
        services.AddScoped<ITermRepository, TermRepository>();
        services.AddScoped<ITermAdminService, TermAdminService>();
        services.AddScoped<ITermQueryService, TermQueryService>();
        return services;
    }
}
