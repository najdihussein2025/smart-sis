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
        builder.Properties<DateTime>().HaveColumnType("datetime2");
    }

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
