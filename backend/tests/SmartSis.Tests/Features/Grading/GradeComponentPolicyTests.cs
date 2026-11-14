using SmartSis.Api.Features.Grading.Rules;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Grading;

/// <summary>Rule G1: Σ weight of a course's components in a term ≤ 100.</summary>
public sealed class GradeComponentPolicyTests
{
    [Fact]
    public void CheckTotalWeight_ExactlyHundred_Passes() =>
        GradeComponentPolicy.CheckTotalWeight([30m, 30m], 40m);

    [Fact]
    public void CheckTotalWeight_NoOtherComponents_FullWeightPasses() =>
        GradeComponentPolicy.CheckTotalWeight([], 100m);

    [Fact]
    public void CheckTotalWeight_JustOverHundred_Throws()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => GradeComponentPolicy.CheckTotalWeight([30m, 30m], 40.01m));
        Assert.Equal(GradeComponentPolicy.WeightOver100, ex.Code);
        Assert.Contains("100.01%", ex.Message);
        Assert.Contains("40% is left", ex.Message);
    }

    [Fact]
    public void CheckTotalWeight_UpdateLeavesOwnWeightOut_Passes() =>
        // Editing a 40% component to 45% when the others weigh 55%: total 100.
        GradeComponentPolicy.CheckTotalWeight([25m, 30m], 45m);
}
