using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Interfaces;

/// <summary>Student side of support (PB-13): the signed-in student's own requests only.</summary>
public interface ISupportService
{
    Task<List<SupportCategoryDto>> ListCategoriesAsync(CancellationToken ct);
    Task<PagedResult<SupportRequestSummaryDto>> ListMineAsync(MySupportRequestsQuery query, CancellationToken ct);
    Task<SupportRequestDetailDto> GetMineAsync(int id, CancellationToken ct);
    Task<SupportRequestDetailDto> CreateAsync(CreateSupportRequestRequest request, CancellationToken ct);
    Task<SupportRequestDetailDto> ReplyAsync(int id, SupportReplyRequest request, CancellationToken ct);
}
