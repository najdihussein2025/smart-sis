namespace SmartSis.Api.Features.Notifications.Rules;

/// <summary>
/// When EmailOutboxJob tries a queued email again (rule C2). A new email goes out on the next run; after a failed
/// attempt the wait doubles: 1, 2, 4, 8 minutes. After <c>EmailOutboxMessage.MaxAttempts</c> failures the email is Failed.
/// The last attempt time is the row's <c>UpdatedAt</c> (the job is the only writer), so no extra column is needed.
/// </summary>
public static class EmailRetryPolicy
{
    public static readonly TimeSpan BaseDelay = TimeSpan.FromMinutes(1);

    /// <summary>Wait after <paramref name="failedAttempts"/> failures: 0 → none, n → BaseDelay × 2^(n−1).</summary>
    public static TimeSpan DelayAfter(int failedAttempts) =>
        failedAttempts <= 0 ? TimeSpan.Zero : BaseDelay * Math.Pow(2, failedAttempts - 1);

    public static DateTime NextAttemptAt(int failedAttempts, DateTime lastAttemptAt) => lastAttemptAt + DelayAfter(failedAttempts);

    public static bool IsDue(int failedAttempts, DateTime lastAttemptAt, DateTime utcNow) =>
        NextAttemptAt(failedAttempts, lastAttemptAt) <= utcNow;

    /// <summary>
    /// Index n = the latest <c>UpdatedAt</c> at which a queued email with n failed attempts is due at <paramref name="utcNow"/>.
    /// Lets the repository filter due emails in SQL.
    /// </summary>
    public static IReadOnlyList<DateTime> DueCutoffs(DateTime utcNow, int maxAttempts) =>
        Enumerable.Range(0, Math.Max(maxAttempts, 0)).Select(n => utcNow - DelayAfter(n)).ToArray();
}
