namespace SmartSis.Api.Shared.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Runs <paramref name="work"/> in one database transaction. Commits on success, rolls back on any exception.</summary>
    Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken ct = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct = default);
}
