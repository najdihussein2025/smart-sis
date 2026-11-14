using SmartSis.Api.Features.Grading.Rules;

namespace SmartSis.Tests.Features.Grading;

/// <summary>Rules G4 (credit-weighted 4.0 GPA) and G5 (latest retake counts), A3 (absence F counts).</summary>
public sealed class GpaCalculatorTests
{
    private const int Spring = 1, Fall = 2, NextSpring = 3;
    private static readonly int[] Order = [Spring, Fall, NextSpring];

    [Fact]
    public void Gpa_IsCreditWeighted()
    {
        // (4.0 × 3 + 2.0 × 1) / 4 = 3.5
        Assert.Equal(3.5m, GpaCalculator.Gpa([new(Spring, 1, 3, 4.0m), new(Spring, 2, 1, 2.0m)]));
    }

    [Fact]
    public void Gpa_RoundsToTwoDecimals()
    {
        // (4.0 + 3.3 + 3.3) × 3 / 9 = 3.5333… → 3.53
        Assert.Equal(3.53m, GpaCalculator.Gpa([new(Spring, 1, 3, 4.0m), new(Spring, 2, 3, 3.3m), new(Spring, 3, 3, 3.3m)]));
        // (4.0 + 3.7 + 3.3 + 3.3 + 2.7 + 2.0 + 2.0 + 2.0) / 8 = 2.875 → 2.88 (half away from zero)
        Assert.Equal(2.88m, GpaCalculator.Gpa([.. new[] { 4.0m, 3.7m, 3.3m, 3.3m, 2.7m, 2.0m, 2.0m, 2.0m }.Select((p, i) => new GradedAttempt(Spring, i, 1, p))]));
    }

    [Fact]
    public void Gpa_NoCredits_IsZero() => Assert.Equal(0m, GpaCalculator.Gpa([]));

    [Fact]
    [Trait("Rule", "A3")]
    public void Gpa_AbsenceDropF_CountsAsZero()
    {
        // A (4.0, 3 cr) + absence F (0.0, 3 cr) = 2.0
        Assert.Equal(2.0m, GpaCalculator.Gpa([new(Fall, 1, 3, 4.0m), new(Fall, 2, 3, 0m)]));
    }

    [Fact]
    [Trait("Rule", "G5")]
    public void Cumulative_Retake_OnlyLatestAttemptCounts()
    {
        // CSC 101: F in Spring, B in Fall. MAT 201: A in Spring. Latest: B (3.0) + A (4.0) → 3.5, 6 credits, all earned.
        var attempts = new GradedAttempt[] { new(Fall, 101, 3, 3.0m), new(Spring, 101, 3, 0m), new(Spring, 201, 3, 4.0m) };

        var c = GpaCalculator.Cumulative(attempts, Order);

        Assert.Equal(new CumulativeGpa(3.5m, 6, 6), c);
    }

    [Fact]
    [Trait("Rule", "G5")]
    public void Cumulative_LatestAttemptCounts_EvenWhenWorse()
    {
        var c = GpaCalculator.Cumulative([new(Spring, 101, 3, 3.0m), new(Fall, 101, 3, 1.0m)], Order);
        Assert.Equal(1.0m, c.Gpa);
    }

    [Fact]
    public void Cumulative_FailedCourse_AttemptedButNotEarned()
    {
        var c = GpaCalculator.Cumulative([new(Spring, 101, 3, 0m), new(Spring, 201, 4, 1.0m)], Order);
        Assert.Equal(new CumulativeGpa(0.57m, 7, 4), c);
    }

    [Fact]
    [Trait("Rule", "G6")]
    public void ByTerm_OneRowPerGradedTerm_WithCumulativeUpToThatTerm()
    {
        var attempts = new GradedAttempt[]
        {
            new(Spring, 101, 3, 0m),    // F
            new(Spring, 201, 3, 4.0m),  // A
            new(NextSpring, 101, 3, 3.0m), // CSC 101 retaken: B
        };

        var rows = GpaCalculator.ByTerm(attempts, Order);

        Assert.Equal(2, rows.Count); // Fall has no graded attempt
        Assert.Equal(new TermGpaResult(Spring, 2.0m, 2.0m, 6, 3), rows[0]);
        // Term GPA shows the term's own grade; cumulative drops the old F for the retake: (3.0 + 4.0) / 2 = 3.5.
        Assert.Equal(new TermGpaResult(NextSpring, 3.0m, 3.5m, 3, 3), rows[1]);
    }

    [Fact]
    public void ByTerm_NoAttempts_NoRows() => Assert.Empty(GpaCalculator.ByTerm([], Order));
}
