using SmartSis.Api.Features.Cart.Interfaces;
using SmartSis.Api.Features.Cart.Repositories;
using SmartSis.Api.Features.Cart.Services;

namespace SmartSis.Api.Features.Cart;

public static class CartModule
{
    public static IServiceCollection AddCartFeature(this IServiceCollection services)
    {
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartService, CartService>();
        return services;
    }
}
