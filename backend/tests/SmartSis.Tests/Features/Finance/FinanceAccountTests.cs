using SmartSis.Api.Features.Finance.Rules;

namespace SmartSis.Tests.Features.Finance;

/// <summary>Account totals and the overdue flag shown on the student's financial account.</summary>
public sealed class FinanceAccountTests
{
    private static readonly DateOnly Today = new(2026, 10, 15);

    [Fact]
    public void IsOverdue_DueYesterdayWithBalance_True() => Assert.True(FinanceAccount.IsOverdue(10m, Today.AddDays(-1), Today));

    [Fact]
    public void IsOverdue_DueToday_False() => Assert.False(FinanceAccount.IsOverdue(10m, Today, Today));

    [Fact]
    public void IsOverdue_PastDueButPaid_False() => Assert.False(FinanceAccount.IsOverdue(0m, Today.AddDays(-30), Today));

    [Fact]
    public void Summarize_MixedCharges_SumsChargedPaidOutstandingAndOverdue()
    {
        var totals = FinanceAccount.Summarize(
        [
            new ChargeLine(1000m, 400m, Today.AddDays(-3)), // partial, overdue: 600 owed
            new ChargeLine(250m, 250m, Today.AddDays(-60)), // paid long ago: not overdue
            new ChargeLine(150m, 0m, Today.AddDays(10)),    // pending, due later
            new ChargeLine(80m, 0m, Today),                 // pending, due today (not overdue)
        ], Today);

        Assert.Equal(1480m, totals.TotalCharged);
        Assert.Equal(650m, totals.TotalPaid);
        Assert.Equal(830m, totals.Outstanding);
        Assert.Equal(600m, totals.Overdue);
        Assert.Equal(Today, totals.NextDueDate);
    }

    [Fact]
    public void Summarize_EverythingPaid_NoBalanceAndNoNextDueDate()
    {
        var totals = FinanceAccount.Summarize([new ChargeLine(500m, 500m, Today.AddDays(5))], Today);
        Assert.Equal(0m, totals.Outstanding);
        Assert.Equal(0m, totals.Overdue);
        Assert.Null(totals.NextDueDate);
    }

    [Fact]
    public void Summarize_NoCharges_AllZero()
    {
        var totals = FinanceAccount.Summarize([], Today);
        Assert.Equal(new AccountTotals(0m, 0m, 0m, 0m, null), totals);
    }

    [Fact]
    public void Summarize_OnlyOverdueOpenCharges_NextDueDateIsNull()
    {
        var totals = FinanceAccount.Summarize([new ChargeLine(100m, 0m, Today.AddDays(-1))], Today);
        Assert.Equal(100m, totals.Overdue);
        Assert.Null(totals.NextDueDate);
    }
}
