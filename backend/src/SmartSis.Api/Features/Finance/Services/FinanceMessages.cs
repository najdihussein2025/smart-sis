using System.Globalization;
using System.Net;

namespace SmartSis.Api.Features.Finance.Services;

/// <summary>Notification and email texts for charges and payments. Every value is HTML-encoded in the emails.</summary>
internal static class FinanceMessages
{
    public const string Link = "/student/finance";

    public static string Money(decimal amount) => amount.ToString("$#,##0.00", CultureInfo.InvariantCulture);

    public static string Day(DateOnly date) => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    public static (string Title, string Body) ChargedNotification(string title, decimal amount, DateOnly due) =>
        ($"New charge: {title}", $"{Money(amount)} is due on {Day(due)}.");

    public static (string Subject, string Html) ChargedEmail(string fullName, string title, decimal amount, DateOnly due) => (
        $"New charge on your account: {title}",
        Wrap($"""
            <p>Hello {E(fullName)},</p>
            <p>A charge was added to your account: <strong>{E(title)}</strong>, {E(Money(amount))}, due on {E(Day(due))}.</p>
            <p>You can see your balance and due dates under Financial account in Smart SIS.</p>
            """));

    public static (string Title, string Body) PaymentNotification(string title, decimal amount, decimal balance) =>
        ("Payment recorded", balance > 0
            ? $"{Money(amount)} received for {title}. Remaining balance: {Money(balance)}."
            : $"{Money(amount)} received for {title}. This charge is now fully paid.");

    public static (string Subject, string Html) PaymentEmail(string fullName, string title, decimal amount, decimal balance) => (
        $"Payment received: {title}",
        Wrap($"""
            <p>Hello {E(fullName)},</p>
            <p>We recorded a payment of <strong>{E(Money(amount))}</strong> for <strong>{E(title)}</strong>.</p>
            <p>{(balance > 0 ? $"Remaining balance on this charge: {E(Money(balance))}." : "This charge is now fully paid.")}</p>
            """));

    private static string E(string s) => WebUtility.HtmlEncode(s);

    private static string Wrap(string inner) =>
        $"""<div style="font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#1f2937">{inner}<p style="color:#6b7280">Smart SIS · Antonine University</p></div>""";
}
