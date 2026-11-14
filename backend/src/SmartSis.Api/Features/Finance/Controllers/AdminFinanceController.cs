using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Finance.Interfaces;
using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Controllers;

/// <summary>Admin: charge a whole major at once, one-off charges, and full or partial payments (PB-18).</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/finance")]
[Tags("Admin: Finance")]
public sealed class AdminFinanceController(IFinanceAdminService finance) : ControllerBase
{
    /// <summary>Terms, majors (with their active student count) and payment types for the forms.</summary>
    [HttpGet("options")]
    public Task<FinanceOptionsDto> Options(CancellationToken ct) => finance.GetOptionsAsync(ct);

    /// <summary>Charge batches, newest first, with how many of their charges are paid.</summary>
    [HttpGet("batches")]
    public Task<PagedResult<ChargeBatchDto>> Batches([FromQuery] ChargeBatchSearchQuery query, CancellationToken ct) =>
        finance.SearchBatchesAsync(query, ct);

    /// <summary>One charge batch.</summary>
    /// <response code="404">No batch with this id.</response>
    [HttpGet("batches/{id:int}")]
    [ProducesResponseType<ChargeBatchDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ChargeBatchDto> Batch(int id, CancellationToken ct) => finance.GetBatchAsync(id, ct);

    /// <summary>Charge every active student of a major (rule F1). Each student gets a notification and an email.</summary>
    /// <response code="201">Created; <c>chargeCount</c> says how many students were charged.</response>
    /// <response code="400">Invalid fields, or a major / term / payment type that doesn't exist.</response>
    /// <response code="409"><c>no_active_students</c>: the major has nobody to charge.</response>
    [HttpPost("batches")]
    [ProducesResponseType<ChargeBatchDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ChargeBatchDto>> CreateBatch(CreateChargeBatchRequest request, CancellationToken ct)
    {
        var batch = await finance.CreateBatchAsync(request, ct);
        return CreatedAtAction(nameof(Batch), new { id = batch.Id }, batch);
    }

    /// <summary>Search charges: unpaid first, then by due date. Filter by student, status, term, major or batch.</summary>
    [HttpGet("charges")]
    public Task<PagedResult<AdminChargeDto>> Charges([FromQuery] ChargeSearchQuery query, CancellationToken ct) =>
        finance.SearchChargesAsync(query, ct);

    /// <summary>One charge with its payments.</summary>
    /// <response code="404">No charge with this id.</response>
    [HttpGet("charges/{id:int}")]
    [ProducesResponseType<AdminChargeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AdminChargeDto> Charge(int id, CancellationToken ct) => finance.GetChargeAsync(id, ct);

    /// <summary>A one-off charge for a single student. The student gets a notification and an email.</summary>
    /// <response code="201">Created.</response>
    /// <response code="400">Invalid fields, or no active student with this University ID.</response>
    [HttpPost("charges")]
    [ProducesResponseType<AdminChargeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminChargeDto>> CreateCharge(CreateChargeRequest request, CancellationToken ct)
    {
        var charge = await finance.CreateChargeAsync(request, ct);
        return CreatedAtAction(nameof(Charge), new { id = charge.Id }, charge);
    }

    /// <summary>Record a full or partial payment; the charge status becomes Partial or Paid (rule F2).</summary>
    /// <response code="400">Invalid amount or note.</response>
    /// <response code="404">No charge with this id.</response>
    /// <response code="409"><c>overpayment</c> (more than the remaining balance, rule F3) or <c>already_paid</c>.</response>
    [HttpPost("charges/{id:int}/payments")]
    [ProducesResponseType<AdminChargeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<AdminChargeDto> RecordPayment(int id, RecordPaymentRequest request, CancellationToken ct) =>
        finance.RecordPaymentAsync(id, request, ct);
}
