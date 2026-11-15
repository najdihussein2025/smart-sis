using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Rules;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>Rules A1, A2, A3 and A5 at the boundaries (limit − 1 / limit / limit + 1). A 3-credit course has a limit of 9.</summary>
public sealed class AbsencePolicyTests
{
    private const int Limit = 9;
    private static readonly AbsenceAlertLevel[] NoneSent = [];

    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 9)]
    [InlineData(4, 12)]
    [Trait("Rule", "A1")]
    public void LimitFor_IsCreditsTimesThree(int credits, int limit) => Assert.Equal(limit, AbsencePolicy.LimitFor(credits));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [Trait("Rule", "A2")]
    public void Evaluate_BelowLimitMinusOne_NothingDue(int absences) =>
        Assert.Equal(AbsenceDecision.None, AbsencePolicy.Evaluate(absences, Limit, NoneSent));

    [Fact]
    [Trait("Rule", "A2")]
    public void Evaluate_AtLimitMinusOne_SendsWarning() =>
        Assert.Equal(new AbsenceDecision(AbsenceAlertLevel.Warning, false), AbsencePolicy.Evaluate(8, Limit, NoneSent));

    [Fact]
    [Trait("Rule", "A2")]
    public void Evaluate_AtLimitMinusOne_WarningAlreadySent_NothingDue() =>
        Assert.Equal(AbsenceDecision.None, AbsencePolicy.Evaluate(8, Limit, [AbsenceAlertLevel.Warning]));

    [Fact]
    [Trait("Rule", "A2")]
    public void Evaluate_AtLimit_SendsLimitAlert() =>
        Assert.Equal(new AbsenceDecision(AbsenceAlertLevel.Limit, false), AbsencePolicy.Evaluate(9, Limit, [AbsenceAlertLevel.Warning]));

    [Fact]
    [Trait("Rule", "A2")]
    public void Evaluate_AtLimit_LimitAlreadySent_NothingDue() =>
        Assert.Equal(AbsenceDecision.None, AbsencePolicy.Evaluate(9, Limit, [AbsenceAlertLevel.Warning, AbsenceAlertLevel.Limit]));

    [Fact]
    [Trait("Rule", "A3")]
    public void Evaluate_AtLimitPlusOne_AutoDropsAndTellsTheStudent() =>
        Assert.Equal(new AbsenceDecision(AbsenceAlertLevel.AutoDropped, true),
            AbsencePolicy.Evaluate(10, Limit, [AbsenceAlertLevel.Warning, AbsenceAlertLevel.Limit]));

    [Fact]
    [Trait("Rule", "A3")]
    public void Evaluate_OverLimit_AlreadyToldAboutTheDrop_StillDropsWithoutASecondAlert() =>
        Assert.Equal(new AbsenceDecision(null, true), AbsencePolicy.Evaluate(11, Limit, [AbsenceAlertLevel.AutoDropped]));

    [Theory]
    [InlineData(0, AbsenceStanding.Perfect)]
    [InlineData(1, AbsenceStanding.Warning)]
    [InlineData(7, AbsenceStanding.Warning)]   // limit − 2
    [InlineData(8, AbsenceStanding.Critical)]  // limit − 1
    [InlineData(9, AbsenceStanding.Critical)]  // limit
    [InlineData(10, AbsenceStanding.Critical)] // dropped
    [Trait("Rule", "A5")]
    public void StandingFor_FollowsTheColourRule(int absences, AbsenceStanding expected) =>
        Assert.Equal(expected, AbsencePolicy.StandingFor(absences, Limit));

    [Theory]
    [InlineData(0, AbsenceStanding.Perfect)]
    [InlineData(1, AbsenceStanding.Warning)]
    [InlineData(2, AbsenceStanding.Critical)]
    [Trait("Rule", "A5")]
    public void StandingFor_OneCreditCourse_LimitThree(int absences, AbsenceStanding expected) =>
        Assert.Equal(expected, AbsencePolicy.StandingFor(absences, AbsencePolicy.LimitFor(1)));
}
