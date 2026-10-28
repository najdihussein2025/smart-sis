using SmartSis.Api.Features.Auth.Interfaces;
using SmartSis.Api.Features.Auth.Repositories;
using SmartSis.Api.Features.Auth.Services;

namespace SmartSis.Api.Features.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
