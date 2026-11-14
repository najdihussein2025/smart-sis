using SmartSis.Api.Features.Announcements.Interfaces;
using SmartSis.Api.Features.Announcements.Jobs;
using SmartSis.Api.Features.Announcements.Repositories;
using SmartSis.Api.Features.Announcements.Services;

namespace SmartSis.Api.Features.Announcements;

public static class AnnouncementsModule
{
    public static IServiceCollection AddAnnouncementsFeature(this IServiceCollection services)
    {
        services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddHostedService<AnnouncementPublishJob>();
        return services;
    }
}
