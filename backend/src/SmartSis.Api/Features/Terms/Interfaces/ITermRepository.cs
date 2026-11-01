using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Terms.Interfaces;

public interface ITermRepository : IRepository<Term>
{
    /// <summary>All terms, newest <see cref="Term.StartDate"/> first. Not tracked.</summary>
    Task<IReadOnlyList<Term>> ListAsync(CancellationToken ct);

    /// <summary>Case-insensitive name check, optionally ignoring one term (the one being updated).</summary>
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);

    /// <summary>The current term (tracked), optionally ignoring one term.</summary>
    Task<Term?> GetCurrentAsync(int? excludeId, CancellationToken ct);
}
