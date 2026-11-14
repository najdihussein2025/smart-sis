using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Grading.Rules;

/// <summary>Rule G1: the components of one course in one term weigh at most 100% together. Pure.</summary>
public static class GradeComponentPolicy
{
    public const decimal MaxTotalWeight = 100m;

    public const string WeightOver100 = "weight_over_100";

    /// <summary>Checked on insert and on update (with the edited component left out of <paramref name="otherWeights"/>).</summary>
    /// <param name="otherWeights">Weights of the course's other components in the term.</param>
    /// <param name="weight">Weight of the component being saved.</param>
    public static void CheckTotalWeight(IEnumerable<decimal> otherWeights, decimal weight)
    {
        var others = otherWeights.Sum();
        var total = others + weight;
        if (total > MaxTotalWeight)
            throw new BusinessRuleException(
                $"The weights would add up to {total:0.##}%, but a course can't go over 100%. {MaxTotalWeight - others:0.##}% is left.",
                WeightOver100);
    }
}
