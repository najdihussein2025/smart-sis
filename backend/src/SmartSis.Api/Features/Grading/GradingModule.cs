using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Repositories;
using SmartSis.Api.Features.Grading.Services;

namespace SmartSis.Api.Features.Grading;

public static class GradingModule
{
    public static IServiceCollection AddGradingFeature(this IServiceCollection services)
    {
        services.AddScoped<IGradingRepository, GradingRepository>();
        services.AddScoped<IGradeComponentService, GradeComponentService>();
        services.AddScoped<IGradeEntryService, GradeEntryService>();
        services.AddScoped<ITermResultService, TermResultService>();
        return services;
    }
}
