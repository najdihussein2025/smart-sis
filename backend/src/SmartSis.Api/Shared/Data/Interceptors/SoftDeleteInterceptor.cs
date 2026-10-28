using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Shared.Data.Interceptors;

/// <summary>Turns Remove() on an ISoftDeletable entity into an update (D1: no hard deletes).</summary>
public sealed class SoftDeleteInterceptor(TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Convert(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Convert(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Convert(DbContext? context)
    {
        if (context is null) return;
        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;
            entry.State = EntityState.Modified;
            entry.Entity.MarkDeleted(now);
        }
    }
}
