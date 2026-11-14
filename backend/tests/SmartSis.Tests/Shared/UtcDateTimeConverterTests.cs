using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Announcements.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Tests.Shared;

/// <summary>
/// PB-23 regression: SQL Server returns datetime2 values as Unspecified, so the API sent term windows without "Z" and
/// browsers shifted them by the viewer's offset. Every DateTime column must come back as UTC.
/// </summary>
public class UtcDateTimeConverterTests
{
    private static SisDbContext SqlServerModel() =>
        new(new DbContextOptionsBuilder<SisDbContext>()
            .UseSqlServer("Server=(localdb)\\unused;Database=none;Trusted_Connection=True") // model only, never opened
            .Options);

    [Fact]
    public void Read_UnspecifiedFromDatabase_IsUtc()
    {
        var converter = new SisDbContext.UtcDateTimeConverter();
        var fromDb = new DateTime(2026, 12, 1, 6, 0, 0, DateTimeKind.Unspecified);

        var value = (DateTime)converter.ConvertFromProvider(fromDb)!;

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(fromDb.Ticks, value.Ticks); // same instant, only the kind changes
    }

    [Fact]
    public void Write_UtcValue_IsStoredUnchanged()
    {
        var converter = new SisDbContext.UtcDateTimeConverter();
        var utc = new DateTime(2026, 12, 1, 6, 0, 0, DateTimeKind.Utc);

        Assert.Equal(utc, (DateTime)converter.ConvertToProvider(utc)!);
    }

    [Theory]
    [InlineData(typeof(Term), nameof(Term.EnrollStart))]
    [InlineData(typeof(Term), nameof(Term.AddDropEnd))]
    [InlineData(typeof(Announcement), nameof(Announcement.ExpiresAt))] // nullable DateTime
    public void Model_DateTimeColumns_UseUtcConverter(Type entity, string property)
    {
        using var db = SqlServerModel();

        var converter = db.Model.FindEntityType(entity)!.FindProperty(property)!.GetValueConverter();

        Assert.NotNull(converter);
        var read = converter.ConvertFromProvider(new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Unspecified));
        Assert.Equal(DateTimeKind.Utc, ((DateTime)read!).Kind);
    }
}
