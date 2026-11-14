using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Interfaces;

/// <summary>Admin side of PB-18: charge batches, one-off charges and payments.</summary>
public interface IFinanceAdminService
{
    Task<FinanceOptionsDto> GetOptionsAsync(CancellationToken ct);
    Task<PagedResult<ChargeBatchDto>> SearchBatchesAsync(ChargeBatchSearchQuery query, CancellationToken ct);
    Task<ChargeBatchDto> GetBatchAsync(int id, CancellationToken ct);
    Task<ChargeBatchDto> CreateBatchAsync(CreateChargeBatchRequest request, CancellationToken ct);
    Task<PagedResult<AdminChargeDto>> SearchChargesAsync(ChargeSearchQuery query, CancellationToken ct);
    Task<AdminChargeDto> GetChargeAsync(int id, CancellationToken ct);
    Task<AdminChargeDto> CreateChargeAsync(CreateChargeRequest request, CancellationToken ct);
    Task<AdminChargeDto> RecordPaymentAsync(int chargeId, RecordPaymentRequest request, CancellationToken ct);
}

/// <summary>Student side of PB-18. The student always comes from the token.</summary>
public interface IMyFinanceService
{
    Task<MyFinanceDto> GetMineAsync(CancellationToken ct);
}
