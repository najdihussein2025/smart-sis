using SmartSis.Api.Features.Terms.Interfaces;
using SmartSis.Api.Features.Terms.Models.Dtos;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Terms.Services;

public sealed class TermQueryService(ITermRepository terms, TimeProvider clock) : ITermQueryService
{
    public async Task<TermDto> GetCurrentAsync(CancellationToken ct)
    {
        var term = await terms.GetCurrentAsync(excludeId: null, ct) ?? throw new NotFoundException("No term is current right now.");
        return term.ToDto(clock.GetUtcNow().UtcDateTime);
    }
}
