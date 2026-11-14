using SmartSis.Api.Features.Enrollments.Interfaces;
using SmartSis.Api.Features.Enrollments.Repositories;
using SmartSis.Api.Features.Enrollments.Services;

namespace SmartSis.Api.Features.Enrollments;

public static class EnrollmentsModule
{
    public static IServiceCollection AddEnrollmentsFeature(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        return services;
    }
}
