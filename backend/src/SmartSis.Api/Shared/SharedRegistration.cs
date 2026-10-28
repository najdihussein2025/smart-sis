using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Data.Interceptors;
using SmartSis.Api.Shared.Email;
using SmartSis.Api.Shared.Repositories;
using SmartSis.Api.Shared.Web;

namespace SmartSis.Api.Shared;

/// <summary>Registers plumbing used by every feature. No business logic here.</summary>
public static class SharedRegistration
{
    public const string FrontendCorsPolicy = "Frontend";

    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();

        // Database
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();
        services.AddDbContext<SisDbContext>((sp, options) =>
        {
            options.UseSqlServer(config.GetConnectionString("Sis"));
            options.AddInterceptors(
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<AuditInterceptor>());
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Auth
        services.Configure<JwtSettings>(config.GetSection(JwtSettings.Section));
        var jwt = config.GetSection(JwtSettings.Section).Get<JwtSettings>() ?? new JwtSettings();
        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be set and at least 32 characters (use user-secrets or the Jwt__Key environment variable).");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = SisClaims.Name,
                    RoleClaimType = SisClaims.Role,
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Student, p => p.RequireRole(Roles.Student))
            .AddPolicy(Policies.Instructor, p => p.RequireRole(Roles.Instructor))
            .AddPolicy(Policies.Admin, p => p.RequireRole(Roles.Admin))
            .AddPolicy(Policies.Staff, p => p.RequireRole(Roles.Instructor, Roles.Admin));

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Email (the outbox job lives in Features/Notifications)
        services.Configure<EmailSettings>(config.GetSection(EmailSettings.Section));
        var provider = config.GetValue<string>("Email:Provider");
        if (string.Equals(provider, "Smtp", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LogEmailSender>();

        // Web
        services.AddControllers(o => o.Filters.Add<FluentValidationFilter>());
        services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
        services.AddProblemDetails();
        services.AddOpenApi();

        var frontendUrl = config.GetValue<string>("Frontend:BaseUrl") ?? "http://localhost:5173";
        services.AddCors(o => o.AddPolicy(FrontendCorsPolicy, p => p
            .WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
