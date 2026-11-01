using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Terms.Interfaces;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Terms.Repositories;

public sealed class TermRepository(SisDbContext db) : Repository<Term>(db), ITermRepository
{
    public async Task<IReadOnlyList<Term>> ListAsync(CancellationToken ct) =>
        await Set.AsNoTracking().OrderByDescending(t => t.StartDate).ThenBy(t => t.Name).ToListAsync(ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct)
    {
        // ToLower on both sides: SQL Server's collation is case-insensitive anyway, EF InMemory (tests) is not.
        var lower = name.ToLower();
        return Set.AnyAsync(t => t.Name.ToLower() == lower && (excludeId == null || t.Id != excludeId), ct);
    }

    public Task<Term?> GetCurrentAsync(int? excludeId, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(t => t.IsCurrent && (excludeId == null || t.Id != excludeId), ct);
}
