using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Finance.Interfaces;
using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Features.Finance.Rules;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Services;

/// <summary>
/// Admin finance flows. Charges and their notifications / queued emails are saved in one transaction, so a student is
/// never told about a charge that wasn't created. Check order: referenced ids exist (400) → business rules (409).
/// </summary>
public sealed class FinanceAdminService(
    IFinanceRepository finance,
    INotificationService notifications,
    IEmailQueue emails,
    ICurrentUser me,
    IUnitOfWork uow,
    TimeProvider clock) : IFinanceAdminService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private DateOnly Today => DateOnly.FromDateTime(Now);

    public Task<FinanceOptionsDto> GetOptionsAsync(CancellationToken ct) => finance.GetOptionsAsync(ct);

    public Task<PagedResult<ChargeBatchDto>> SearchBatchesAsync(ChargeBatchSearchQuery query, CancellationToken ct) =>
        finance.SearchBatchesAsync(query.TermId, query.MajorId, new PageQuery(query.Page, query.PageSize), ct);

    public async Task<ChargeBatchDto> GetBatchAsync(int id, CancellationToken ct) =>
        await finance.GetBatchAsync(id, ct) ?? throw NotFoundException.For<ChargeBatch>(id);

    public async Task<ChargeBatchDto> CreateBatchAsync(CreateChargeBatchRequest r, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        var majorName = await finance.MajorNameAsync(r.MajorId, ct);
        if (majorName is null) failures.Add(new("majorId", $"Major {r.MajorId} doesn't exist."));
        await CheckTermAndTypeAsync(r.TermId, r.PaymentTypeId, failures, ct);
        if (failures.Count > 0) throw new ValidationException(failures);

        var title = r.Title.Trim();
        var batch = new ChargeBatch
        {
            MajorId = r.MajorId, TermId = r.TermId, PaymentTypeId = r.PaymentTypeId, Title = title,
            Amount = r.Amount, DueDate = r.DueDate, CreatedBy = me.UserId,
        };

        await uow.ExecuteInTransactionAsync(async () =>
        {
            // Rule F1: one charge per ACTIVE student of the major, read inside the transaction.
            var students = await finance.ActiveStudentsOfMajorAsync(r.MajorId, ct);
            if (students.Count == 0)
                throw new BusinessRuleException($"{majorName} has no active students to charge.", "no_active_students");

            finance.Add(batch);
            foreach (var s in students)
            {
                finance.Add(new Charge
                {
                    StudentId = s.UserId, Batch = batch, TermId = r.TermId, PaymentTypeId = r.PaymentTypeId,
                    Title = title, Amount = r.Amount, DueDate = r.DueDate,
                });
                NotifyCharged(s, title, r.Amount, r.DueDate);
            }
        }, ct);

        return await GetBatchAsync(batch.Id, ct);
    }

    public async Task<PagedResult<AdminChargeDto>> SearchChargesAsync(ChargeSearchQuery query, CancellationToken ct)
    {
        var page = await finance.SearchChargesAsync(query, new PageQuery(query.Page, query.PageSize), ct);
        var today = Today;
        return new PagedResult<AdminChargeDto>(page.Items.Select(c => ToAdminDto(c, today)).ToList(), page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<AdminChargeDto> GetChargeAsync(int id, CancellationToken ct)
    {
        var row = await finance.GetChargeAsync(id, ct) ?? throw NotFoundException.For<Charge>(id);
        return ToAdminDto(row, Today);
    }

    public async Task<AdminChargeDto> CreateChargeAsync(CreateChargeRequest r, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        var student = await finance.ActiveStudentByUniversityIdAsync(r.UniversityId, ct);
        if (student is null) failures.Add(new("universityId", "No active student has this University ID."));
        await CheckTermAndTypeAsync(r.TermId, r.PaymentTypeId, failures, ct);
        if (failures.Count > 0) throw new ValidationException(failures);

        var title = r.Title.Trim();
        var charge = new Charge
        {
            StudentId = student!.UserId, TermId = r.TermId, PaymentTypeId = r.PaymentTypeId,
            Title = title, Amount = r.Amount, DueDate = r.DueDate,
        };
        finance.Add(charge);
        NotifyCharged(student, title, r.Amount, r.DueDate);
        await uow.SaveChangesAsync(ct);

        return await GetChargeAsync(charge.Id, ct);
    }

    public async Task<AdminChargeDto> RecordPaymentAsync(int chargeId, RecordPaymentRequest r, CancellationToken ct)
    {
        var adminId = me.UserId;
        await uow.ExecuteInTransactionAsync(async () =>
        {
            var charge = await finance.LockChargeAsync(chargeId, ct) ?? throw NotFoundException.For<Charge>(chargeId);
            // Rules F2 + F3: rejects an overpayment (409) and recomputes Pending → Partial → Paid.
            charge.ApplyPayment(r.Amount, adminId, Now, string.IsNullOrWhiteSpace(r.Note) ? null : r.Note.Trim());

            var student = await finance.ActiveStudentAsync(charge.StudentId, ct);
            if (student is not null)
            {
                var (title, body) = FinanceMessages.PaymentNotification(charge.Title, r.Amount, charge.Balance);
                notifications.Notify(student.UserId, NotificationType.Payment, title, body, FinanceMessages.Link);
                var (subject, html) = FinanceMessages.PaymentEmail(student.FullName, charge.Title, r.Amount, charge.Balance);
                emails.Enqueue(student.Email, subject, html);
            }
        }, ct);

        return await GetChargeAsync(chargeId, ct);
    }

    private async Task CheckTermAndTypeAsync(int termId, int paymentTypeId, List<ValidationFailure> failures, CancellationToken ct)
    {
        if (!await finance.TermExistsAsync(termId, ct)) failures.Add(new("termId", $"Term {termId} doesn't exist."));
        if (!await finance.PaymentTypeExistsAsync(paymentTypeId, ct)) failures.Add(new("paymentTypeId", $"Payment type {paymentTypeId} doesn't exist."));
    }

    private void NotifyCharged(ChargeRecipient s, string title, decimal amount, DateOnly due)
    {
        var (nTitle, nBody) = FinanceMessages.ChargedNotification(title, amount, due);
        notifications.Notify(s.UserId, NotificationType.Payment, nTitle, nBody, FinanceMessages.Link);
        var (subject, html) = FinanceMessages.ChargedEmail(s.FullName, title, amount, due);
        emails.Enqueue(s.Email, subject, html);
    }

    private static AdminChargeDto ToAdminDto(ChargeRow c, DateOnly today)
    {
        var balance = Math.Max(0, c.Amount - c.Paid);
        return new AdminChargeDto(
            c.Id, c.StudentId, c.UniversityId, c.StudentName, c.MajorName, c.BatchId, c.TermId, c.TermName, c.PaymentTypeName, c.Title,
            c.Amount, c.Paid, balance, c.DueDate, c.Status, FinanceAccount.IsOverdue(balance, c.DueDate, today), c.Payments);
    }
}

/// <summary>The signed-in student's financial account (read only). Also called by the AI chatbot with the student's own token.</summary>
public sealed class MyFinanceService(IFinanceRepository finance, ICurrentUser me, TimeProvider clock) : IMyFinanceService
{
    public async Task<MyFinanceDto> GetMineAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var rows = await finance.ListForStudentAsync(me.UserId, ct);
        var totals = FinanceAccount.Summarize(rows.Select(r => new ChargeLine(r.Amount, r.Paid, r.DueDate)), today);
        var charges = rows.Select(c =>
        {
            var balance = Math.Max(0, c.Amount - c.Paid);
            return new StudentChargeDto(
                c.Id, c.Title, c.TermId, c.TermName, c.PaymentTypeName, c.Amount, c.Paid, balance, c.DueDate, c.Status,
                FinanceAccount.IsOverdue(balance, c.DueDate, today), c.Payments);
        }).ToList();
        return new MyFinanceDto(totals.TotalCharged, totals.TotalPaid, totals.Outstanding, totals.Overdue, totals.NextDueDate, charges);
    }
}
