using SmartSis.Api.Features.Terms.Interfaces;
using SmartSis.Api.Features.Terms.Models.Dtos;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Terms.Services;

public sealed class TermAdminService(ITermRepository terms, IUnitOfWork uow, TimeProvider clock) : ITermAdminService
{
    private DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<TermDto>> ListAsync(CancellationToken ct)
    {
        var now = UtcNow;
        return (await terms.ListAsync(ct)).Select(t => t.ToDto(now)).ToList();
    }

    public async Task<TermDto> GetAsync(int id, CancellationToken ct) =>
        (await FindOrThrowAsync(id, ct)).ToDto(UtcNow);

    public async Task<TermDto> CreateAsync(SaveTermRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, excludeId: null, ct);

        var term = new Term();
        await SaveAsync(term, request, name, isNew: true, ct);
        return term.ToDto(UtcNow);
    }

    public async Task<TermDto> UpdateAsync(int id, SaveTermRequest request, CancellationToken ct)
    {
        var term = await FindOrThrowAsync(id, ct);
        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, excludeId: id, ct);

        await SaveAsync(term, request, name, isNew: false, ct);
        return term.ToDto(UtcNow);
    }

    private async Task SaveAsync(Term term, SaveTermRequest r, string name, bool isNew, CancellationToken ct)
    {
        void Apply()
        {
            term.Name = name;
            term.StartDate = r.StartDate;
            term.EndDate = r.EndDate;
            term.EnrollStart = r.EnrollStart;
            term.EnrollEnd = r.EnrollEnd;
            term.AddDropStart = r.AddDropStart;
            term.AddDropEnd = r.AddDropEnd;
            term.IsCurrent = r.IsCurrent;
            if (isNew) terms.Add(term);
        }

        if (!r.IsCurrent)
        {
            Apply();
            await uow.SaveChangesAsync(ct);
            return;
        }

        // Only one current term. The filtered unique index on IsCurrent is checked per statement,
        // so clear the old current term in its own save before writing this one (never rely on EF ordering).
        await uow.ExecuteInTransactionAsync(async () =>
        {
            var current = await terms.GetCurrentAsync(isNew ? null : term.Id, ct);
            if (current is not null)
            {
                current.IsCurrent = false;
                await uow.SaveChangesAsync(ct);
            }
            Apply();
        }, ct); // the transaction saves again before committing
    }

    private async Task EnsureNameFreeAsync(string name, int? excludeId, CancellationToken ct)
    {
        if (await terms.NameExistsAsync(name, excludeId, ct))
            throw new BusinessRuleException($"A term named '{name}' already exists.", "duplicate_term_name");
    }

    private async Task<Term> FindOrThrowAsync(int id, CancellationToken ct) =>
        await terms.FindAsync(id, ct) ?? throw NotFoundException.For<Term>(id);
}
