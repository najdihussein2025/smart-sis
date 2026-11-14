namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// One feature's simulated demo data (SR-23). DevSeeder finds every implementation, runs them in <see cref="Order"/>
/// after the base data (users, terms, courses, sections, demo enrollments) and saves after each one.
/// Put the class in <c>Shared/Data/Seed/&lt;Feature&gt;DevSeed.cs</c>; it needs a public parameterless constructor.
/// Only runs on an empty database, in Development and in the integration tests.
/// </summary>
public interface IDevSeedStep
{
    /// <summary>Lower runs first. Grading 100, Attendance 200, Finance 300, Support 400, Announcements 500, Career 600, Chat 700.</summary>
    int Order { get; }

    Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct);
}
