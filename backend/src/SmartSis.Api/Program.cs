using Scalar.AspNetCore;
using SmartSis.Api.Shared;
using SmartSis.Api.Shared.Data.Seed;
using SmartSis.Api.Shared.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddFeatures();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
    await DevSeeder.RunAsync(app.Services);
}

app.UseCors(SharedRegistration.FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

public partial class Program { }
