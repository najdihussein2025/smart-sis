using SmartSis.Api.Features.Career.Clients;
using SmartSis.Api.Features.Career.Interfaces;
using SmartSis.Api.Features.Career.Repositories;
using SmartSis.Api.Features.Career.Services;

namespace SmartSis.Api.Features.Career;

public static class CareerModule
{
    public static IServiceCollection AddCareerFeature(this IServiceCollection services)
    {
        services.AddOptions<CareerAiOptions>().BindConfiguration(CareerAiOptions.Section);
        // Typed client; tests replace ICareerAiClient with a fake.
        services.AddHttpClient<ICareerAiClient, CareerAiClient>(c => c.Timeout = TimeSpan.FromSeconds(20));

        services.AddScoped<ICareerRepository, CareerRepository>();
        services.AddScoped<ICareerAdminService, CareerAdminService>();
        services.AddScoped<ICareerService, CareerService>();
        return services;
    }
}
