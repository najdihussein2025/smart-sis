namespace SmartSis.Api.Shared.Repositories;

/// <summary>Generic CRUD. Feature repositories add their own queries on top. Never calls SaveChanges.</summary>
public interface IRepository<T> where T : class
{
    IQueryable<T> Query();
    Task<T?> FindAsync(object[] keys, CancellationToken ct = default);
    Task<T?> FindAsync(int id, CancellationToken ct = default);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Remove(T entity);
}
