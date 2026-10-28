using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SmartSis.Api.Features.Auth.Models.Dtos;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Data.Interceptors;
using SmartSis.Api.Shared.Data.Seed;

namespace SmartSis.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory. SQL Server is swapped for EF InMemory and the simulated
/// DevSeeder data is loaded (Development environment, Seed:DevData = true).
/// </summary>
public sealed class SisApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"SmartSisTests-{Guid.NewGuid():N}";

    public const long AdminId = 1001;
    public const long InstructorId = 2001;
    public const long StudentId = 3001;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SisDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SisDbContext>>();
            services.AddDbContext<SisDbContext>((sp, o) => o
                .UseInMemoryDatabase(_dbName)
                .AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>(), sp.GetRequiredService<AuditInterceptor>()));
        });
    }

    public async Task<HttpClient> ClientForAsync(long universityId, string password = DevSeeder.DevPassword)
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(universityId, password));
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return client;
    }

    public async Task WithDbAsync(Func<SisDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<SisDbContext>());
    }
}
