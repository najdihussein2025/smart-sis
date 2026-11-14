using SmartSis.Api.Features.Finance.Enums;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Finance;

/// <summary>Rules F2 and F3.</summary>
public sealed class ChargeTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ApplyPayment_PartialThenRest_GoesPendingPartialPaid()
    {
        var charge = new Charge { Amount = 1000m };
        Assert.Equal(ChargeStatus.Pending, charge.Status);

        charge.ApplyPayment(400m, recordedBy: 1, Now, null);
        Assert.Equal(ChargeStatus.Partial, charge.Status);
        Assert.Equal(600m, charge.Balance);

        charge.ApplyPayment(600m, recordedBy: 1, Now, null);
        Assert.Equal(ChargeStatus.Paid, charge.Status);
        Assert.Equal(0m, charge.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1000.01)]
    public void ApplyPayment_InvalidAmount_Throws(double amount)
    {
        var charge = new Charge { Amount = 1000m };
        Assert.Throws<BusinessRuleException>(() => charge.ApplyPayment((decimal)amount, 1, Now, null));
    }

    [Fact]
    public void ApplyPayment_MoreThanBalance_ThrowsOverpaymentAndKeepsStatus()
    {
        var charge = new Charge { Amount = 1000m };
        charge.ApplyPayment(900m, 1, Now, null);

        var ex = Assert.Throws<BusinessRuleException>(() => charge.ApplyPayment(100.01m, 1, Now, null));
        Assert.Equal("overpayment", ex.Code);
        Assert.Equal(ChargeStatus.Partial, charge.Status);
        Assert.Single(charge.Payments);
    }

    [Fact]
    public void ApplyPayment_ExactBalance_IsPaid()
    {
        var charge = new Charge { Amount = 1000m };
        charge.ApplyPayment(1000m, 1, Now, null);
        Assert.Equal(ChargeStatus.Paid, charge.Status);
    }

    [Fact]
    public void ApplyPayment_AlreadyPaid_ThrowsAlreadyPaid()
    {
        var charge = new Charge { Amount = 50m };
        charge.ApplyPayment(50m, 1, Now, null);
        var ex = Assert.Throws<BusinessRuleException>(() => charge.ApplyPayment(1m, 1, Now, null));
        Assert.Equal("already_paid", ex.Code);
    }
}
