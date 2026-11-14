using SmartSis.Api.Features.Notifications.Rules;

namespace SmartSis.Tests.Features.Notifications;

/// <summary>T48: email outbox retries back off 1, 2, 4, 8 minutes (rule C2).</summary>
public sealed class EmailRetryPolicyTests
{
    private static readonly DateTime T0 = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 8)]
    public void DelayAfter_DoublesEachFailure(int failed, int minutes) =>
        Assert.Equal(TimeSpan.FromMinutes(minutes), EmailRetryPolicy.DelayAfter(failed));

    [Fact]
    public void IsDue_NewEmail_IsDueAtOnce() => Assert.True(EmailRetryPolicy.IsDue(0, T0, T0));

    [Fact]
    public void IsDue_OneSecondBeforeBackoff_IsFalse() =>
        Assert.False(EmailRetryPolicy.IsDue(2, T0, T0.AddMinutes(2).AddSeconds(-1)));

    [Fact]
    public void IsDue_ExactlyAtBackoff_IsTrue() => Assert.True(EmailRetryPolicy.IsDue(2, T0, T0.AddMinutes(2)));

    [Fact]
    public void DueCutoffs_OnePerAttemptCount()
    {
        var cutoffs = EmailRetryPolicy.DueCutoffs(T0, 5);
        Assert.Equal([T0, T0.AddMinutes(-1), T0.AddMinutes(-2), T0.AddMinutes(-4), T0.AddMinutes(-8)], cutoffs);
    }
}
