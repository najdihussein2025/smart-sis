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
        services.AddScoped<IUserAdminService, UserAdminService>();
        return services;
    }
}
