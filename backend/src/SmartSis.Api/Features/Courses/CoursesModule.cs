using SmartSis.Api.Features.Courses.Interfaces;
using SmartSis.Api.Features.Courses.Repositories;
using SmartSis.Api.Features.Courses.Services;

namespace SmartSis.Api.Features.Courses;

public static class CoursesModule
{
    public static IServiceCollection AddCoursesFeature(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseAdminService, CourseAdminService>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<ICatalogService, CatalogService>();
        return services;
    }
}
