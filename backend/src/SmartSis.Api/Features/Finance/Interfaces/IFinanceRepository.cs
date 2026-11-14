using SmartSis.Api.Features.Finance.Enums;
using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Interfaces;

/// <summary>An active student who receives a charge, with what the notification and email need.</summary>
public sealed record ChargeRecipient(int UserId, long UniversityId, string FullName, string Email);

/// <summary>A charge row before the overdue flag is worked out (that needs "today", which the service owns).</summary>
public sealed record ChargeRow(
    int Id, int StudentId, long UniversityId, string StudentName, string MajorName, int? BatchId,
    int TermId, string TermName, string PaymentTypeName, string Title,
    decimal Amount, decimal Paid, DateOnly DueDate, ChargeStatus Status, List<PaymentDto> Payments);

public interface IFinanceRepository
{
    Task<FinanceOptionsDto> GetOptionsAsync(CancellationToken ct);
    Task<string?> MajorNameAsync(int majorId, CancellationToken ct);
    Task<bool> TermExistsAsync(int termId, CancellationToken ct);
    Task<bool> PaymentTypeExistsAsync(int paymentTypeId, CancellationToken ct);

    /// <summary>Rule F1: active (not deactivated) students of the major.</summary>
    Task<List<ChargeRecipient>> ActiveStudentsOfMajorAsync(int majorId, CancellationToken ct);
    Task<ChargeRecipient?> ActiveStudentByUniversityIdAsync(long universityId, CancellationToken ct);
    /// <summary>Null when the student has been deactivated.</summary>
    Task<ChargeRecipient?> ActiveStudentAsync(int userId, CancellationToken ct);

    Task<PagedResult<ChargeBatchDto>> SearchBatchesAsync(int? termId, int? majorId, PageQuery page, CancellationToken ct);
    Task<ChargeBatchDto?> GetBatchAsync(int id, CancellationToken ct);

    Task<PagedResult<ChargeRow>> SearchChargesAsync(ChargeSearchQuery query, PageQuery page, CancellationToken ct);
    Task<ChargeRow?> GetChargeAsync(int id, CancellationToken ct);
    Task<List<ChargeRow>> ListForStudentAsync(int studentId, CancellationToken ct);

    /// <summary>Loads the charge with its payments, holding a row lock until the transaction ends (two admins can't overpay together).</summary>
    Task<Charge?> LockChargeAsync(int id, CancellationToken ct);

    void Add(ChargeBatch batch);
    void Add(Charge charge);
}
