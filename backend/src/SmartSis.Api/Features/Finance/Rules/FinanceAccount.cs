namespace SmartSis.Api.Features.Finance.Rules;

/// <summary>One charge, reduced to what the account totals need.</summary>
public sealed record ChargeLine(decimal Amount, decimal Paid, DateOnly DueDate)
{
    public decimal Balance => Math.Max(0, Amount - Paid);
}

public sealed record AccountTotals(decimal TotalCharged, decimal TotalPaid, decimal Outstanding, decimal Overdue, DateOnly? NextDueDate);

/// <summary>Pure account maths for the student's financial account. "Today" is passed in (UTC date).</summary>
public static class FinanceAccount
{
    /// <summary>A charge is overdue when something is still owed and its due date is before today (due today is not late).</summary>
    public static bool IsOverdue(decimal balance, DateOnly dueDate, DateOnly today) => balance > 0 && dueDate < today;

    public static AccountTotals Summarize(IEnumerable<ChargeLine> charges, DateOnly today)
    {
        var list = charges.ToList();
        var open = list.Where(c => c.Balance > 0).ToList();
        return new AccountTotals(
            TotalCharged: list.Sum(c => c.Amount),
            TotalPaid: list.Sum(c => c.Paid),
            Outstanding: open.Sum(c => c.Balance),
            Overdue: open.Where(c => IsOverdue(c.Balance, c.DueDate, today)).Sum(c => c.Balance),
            NextDueDate: open.Where(c => c.DueDate >= today).Select(c => (DateOnly?)c.DueDate).Min());
    }
}
