using Microsoft.EntityFrameworkCore;

namespace SmartSis.Api.Shared.Data;

public sealed class UnitOfWork(SisDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await work();
            return true;
        }, ct);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct = default)
    {
        // Nested call: join the outer transaction.
        if (db.Database.CurrentTransaction is not null)
            return await work();

        // InMemory provider (tests) has no transactions.
        if (!db.Database.IsRelational())
        {
            var r = await work();
            await db.SaveChangesAsync(ct);
            return r;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await work();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
