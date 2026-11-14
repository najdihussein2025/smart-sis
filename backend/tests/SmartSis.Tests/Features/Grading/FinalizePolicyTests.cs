using SmartSis.Api.Features.Grading.Rules;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Grading;

/// <summary>When a section's grades can be finalized. Check order: finalized → students → components → weights = 100 → scores.</summary>
public sealed class FinalizePolicyTests
{
    private static readonly decimal[] Full = [30m, 70m];

    private static string? CodeOf(Action act) => Assert.Throws<BusinessRuleException>(act).Code;

    [Fact]
    public void Check_EverythingReady_Passes() => FinalizePolicy.Check(3, 0, Full, []);

    [Fact]
    public void Check_NobodyEnrolledButSomeCompleted_AlreadyFinalized() =>
        Assert.Equal(FinalizePolicy.AlreadyFinalized, CodeOf(() => FinalizePolicy.Check(0, 3, Full, [])));

    [Fact]
    public void Check_EmptySection_NoStudents() =>
        Assert.Equal(FinalizePolicy.NoStudents, CodeOf(() => FinalizePolicy.Check(0, 0, Full, [])));

    [Fact]
    public void Check_NoComponents_NoComponents() =>
        Assert.Equal(FinalizePolicy.NoComponents, CodeOf(() => FinalizePolicy.Check(2, 0, [], [])));

    [Fact]
    public void Check_WeightsBelow100_WeightsIncomplete()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => FinalizePolicy.Check(2, 0, [30m, 69.99m], []));
        Assert.Equal(FinalizePolicy.WeightsIncomplete, ex.Code);
        Assert.Contains("99.99%", ex.Message);
    }

    [Fact]
    public void Check_MissingScores_NamesTheFirstAndCountsTheRest()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => FinalizePolicy.Check(2, 0, Full,
            [new("Demo Student 2", "Final"), new("Demo Student 3", "Final"), new("Demo Student 3", "Midterm")]));
        Assert.Equal(FinalizePolicy.ScoresMissing, ex.Code);
        Assert.StartsWith("3 scores are missing (Demo Student 2: Final and 2 more)", ex.Message);
    }

    [Fact]
    public void Check_OneMissingScore_Singular()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => FinalizePolicy.Check(1, 0, Full, [new("Demo Student 2", "Final")]));
        Assert.StartsWith("1 score is missing (Demo Student 2: Final)", ex.Message);
    }

    [Fact]
    public void Check_WeightsCheckedBeforeScores() =>
        Assert.Equal(FinalizePolicy.WeightsIncomplete, CodeOf(() => FinalizePolicy.Check(1, 0, [50m], [new("A", "B")])));
}
