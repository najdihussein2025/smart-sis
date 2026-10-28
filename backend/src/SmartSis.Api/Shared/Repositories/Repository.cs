using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Shared.Repositories;

public class Repository<T>(SisDbContext db) : IRepository<T> where T : class
{
    protected SisDbContext Db { get; } = db;
    protected DbSet<T> Set => Db.Set<T>();

    public IQueryable<T> Query() => Set;
    public Task<T?> FindAsync(object[] keys, CancellationToken ct = default) => Set.FindAsync(keys, ct).AsTask();
    public Task<T?> FindAsync(int id, CancellationToken ct = default) => Set.FindAsync([id], ct).AsTask();
    public void Add(T entity) => Set.Add(entity);
    public void AddRange(IEnumerable<T> entities) => Set.AddRange(entities);
    public void Remove(T entity) => Set.Remove(entity);
}
