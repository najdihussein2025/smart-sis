using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Shared.Data;

/// <summary>
/// One DbContext for the whole API. Entities are registered by the IEntityTypeConfiguration
/// classes in each feature's Data/ folder (picked up by ApplyConfigurationsFromAssembly).
/// </summary>
public sealed class SisDbContext(DbContextOptions<SisDbContext> options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<decimal>().HavePrecision(10, 2);
        builder.Properties<string>().HaveMaxLength(256);
        builder.Properties<DateTime>().HaveColumnType("datetime2").HaveConversion<UtcDateTimeConverter>();
        builder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <summary>
    /// Every DateTime we store is UTC, but SQL Server's datetime2 doesn't keep the kind: values read back as
    /// <see cref="DateTimeKind.Unspecified"/>, the API then sends them without the trailing "Z", and browsers read
    /// them as local time (term windows shifted by the viewer's offset, and shifted again on every save).
    /// Marking them UTC on read fixes that. Writes are unchanged and the provider type stays DateTime, so the
    /// model snapshot and migrations don't change.
    /// </summary>
    public sealed class UtcDateTimeConverter() : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
        v => v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SisDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // D2: never cascade. Deleting a parent with children must fail.
            foreach (var fk in entityType.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            // D1: hide soft-deleted rows everywhere unless IgnoreQueryFilters() is used.
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var p = Expression.Parameter(entityType.ClrType, "e");
                var body = Expression.Equal(
                    Expression.Property(p, nameof(ISoftDeletable.DeletedAt)),
                    Expression.Constant(null, typeof(DateTime?)));
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, p));
            }
        }
    }
}
