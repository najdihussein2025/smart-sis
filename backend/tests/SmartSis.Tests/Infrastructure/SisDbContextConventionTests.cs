using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Tests.Infrastructure;

/// <summary>
/// SQL Server's datetime2 loses DateTimeKind, so without the converter the API returned times without "Z" and the
/// browser shifted them to local time. EF InMemory keeps the kind, so this checks the model instead of a round trip.
/// </summary>
public sealed class SisDbContextConventionTests
{
    private static SisDbContext NewContext() =>
        new(new DbContextOptionsBuilder<SisDbContext>().UseInMemoryDatabase($"conventions-{Guid.NewGuid():N}").Options);

    [Fact]
    public void Model_EveryDateTimeProperty_UsesUtcConverter()
    {
        using var db = NewContext();
        var dateTimes = db.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .ToList();

        Assert.NotEmpty(dateTimes);
        Assert.Contains(dateTimes, p => p.ClrType == typeof(DateTime?));
        Assert.All(dateTimes, p => Assert.IsType<UtcDateTimeConverter>(p.GetValueConverter()));
    }

    [Fact]
    public void UtcDateTimeConverter_ValueFromDatabase_IsMarkedUtc()
    {
        var converter = new UtcDateTimeConverter();
        var stored = new DateTime(2026, 11, 14, 22, 0, 0, DateTimeKind.Unspecified);

        var read = (DateTime)converter.ConvertFromProvider(stored)!;

        Assert.Equal(DateTimeKind.Utc, read.Kind);
        Assert.Equal(stored.Ticks, read.Ticks);
    }

    [Fact]
    public void UtcDateTimeConverter_ValueToDatabase_IsUnchanged()
    {
        var now = new DateTime(2026, 11, 14, 22, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now, (DateTime)new UtcDateTimeConverter().ConvertToProvider(now)!);
    }
}
