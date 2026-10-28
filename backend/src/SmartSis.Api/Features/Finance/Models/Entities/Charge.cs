using SmartSis.Api.Features.Finance.Enums;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Models.Entities;

public class Charge : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    /// <summary>Null for a one-off charge.</summary>
    public int? BatchId { get; set; }
    public ChargeBatch? Batch { get; set; }

    public int TermId { get; set; }
    public Term Term { get; set; } = null!;
    public int PaymentTypeId { get; set; }
    public PaymentType PaymentType { get; set; } = null!;
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public ChargeStatus Status { get; set; } = ChargeStatus.Pending;

    public ICollection<Payment> Payments { get; set; } = [];

    /// <summary>Requires Payments to be loaded.</summary>
    public decimal PaidAmount => Payments.Sum(p => p.Amount);
    public decimal Balance => Amount - PaidAmount;

    /// <summary>Rules F2 + F3. Requires Payments to be loaded.</summary>
    public Payment ApplyPayment(decimal amount, int recordedBy, DateTime paidAt, string? note)
    {
        if (amount <= 0) throw new BusinessRuleException("Payment amount must be greater than zero.");
        if (amount > Balance) throw new BusinessRuleException($"Payment is more than the remaining balance ({Balance:0.00}).");

        var payment = new Payment { Charge = this, Amount = amount, RecordedBy = recordedBy, PaidAt = paidAt, Note = note };
        Payments.Add(payment);
        RecalculateStatus();
        return payment;
    }

    public void RecalculateStatus()
    {
        var paid = PaidAmount;
        Status = paid <= 0 ? ChargeStatus.Pending : paid < Amount ? ChargeStatus.Partial : ChargeStatus.Paid;
    }
}
