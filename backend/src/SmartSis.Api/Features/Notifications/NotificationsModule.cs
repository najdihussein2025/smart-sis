using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Notifications.Jobs;
using SmartSis.Api.Features.Notifications.Repositories;
using SmartSis.Api.Features.Notifications.Services;

namespace SmartSis.Api.Features.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailQueue, EmailQueue>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddHostedService<EmailOutboxJob>();
        return services;
    }
}
