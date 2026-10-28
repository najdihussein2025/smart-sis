using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Repositories;
using SmartSis.Api.Features.Users.Services;

namespace SmartSis.Api.Features.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        // TODO(phase-2): IUserAdminService for admin add / edit / deactivate.
        return services;
    }
}
