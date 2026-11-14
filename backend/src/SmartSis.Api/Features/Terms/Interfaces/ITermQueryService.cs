using SmartSis.Api.Features.Terms.Models.Dtos;

namespace SmartSis.Api.Features.Terms.Interfaces;

/// <summary>Read-only term facts for any signed-in user (registration windows, used by the chatbot's deadlines tool).</summary>
public interface ITermQueryService
{
    /// <summary>The current term with its windows. Throws <c>NotFoundException</c> when no term is current.</summary>
    Task<TermDto> GetCurrentAsync(CancellationToken ct);
}
