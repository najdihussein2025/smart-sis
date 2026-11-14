using SmartSis.Api.Features.Grading.Rules;

namespace SmartSis.Tests.Features.Grading;

/// <summary>Rule G3: weighted sum → rounded to a whole percent (halves away from zero) → grade scale.</summary>
public sealed class GradeCalculatorTests
{
    /// <summary>PROJECT.md §4 grade scale (the same rows GradeScales is seeded with).</summary>
    public static readonly GradeBand[] Scale =
    [
        new("A+", 4.0m, 97, 100), new("A", 4.0m, 93, 96), new("A-", 3.7m, 89, 92), new("B+", 3.3m, 85, 88),
        new("B", 3.0m, 80, 84), new("B-", 2.7m, 77, 79), new("C+", 2.3m, 73, 76), new("C", 2.0m, 70, 72),
        new("C-", 1.7m, 66, 69), new("D+", 1.3m, 63, 65), new("D", 1.0m, 60, 62), new("F", 0.0m, 0, 59),
    ];

    [Theory]
    [InlineData(59.5, 60)]
    [InlineData(59.49, 59)]
    [InlineData(92.5, 93)]
    [InlineData(96.5, 97)]
    [InlineData(0.4, 0)]
    [InlineData(100, 100)]
    public void RoundPercent_HalvesGoUp(decimal raw, int expected) =>
        Assert.Equal(expected, GradeCalculator.RoundPercent(raw));

    [Theory]
    [InlineData(100, "A+", 4.0)]
    [InlineData(97, "A+", 4.0)]
    [InlineData(96, "A", 4.0)]
    [InlineData(93, "A", 4.0)]
    [InlineData(92, "A-", 3.7)]
    [InlineData(89, "A-", 3.7)]
    [InlineData(88, "B+", 3.3)]
    [InlineData(84, "B", 3.0)]
    [InlineData(80, "B", 3.0)]
    [InlineData(79, "B-", 2.7)]
    [InlineData(76, "C+", 2.3)]
    [InlineData(72, "C", 2.0)]
    [InlineData(69, "C-", 1.7)]
    [InlineData(65, "D+", 1.3)]
    [InlineData(62, "D", 1.0)]
    [InlineData(60, "D", 1.0)]
    [InlineData(59, "F", 0.0)]
    [InlineData(0, "F", 0.0)]
    public void Lookup_EveryBandEdge(int pct, string letter, decimal points)
    {
        var band = GradeCalculator.Lookup(pct, Scale);
        Assert.Equal((letter, points), (band.Letter, band.GpaPoints));
    }

    [Fact]
    public void Lookup_OutsideScale_Throws() =>
        Assert.Throws<InvalidOperationException>(() => GradeCalculator.Lookup(101, Scale));

    [Fact]
    public void Final_WeightedSum_UsesScoreOverMaxTimesWeight()
    {
        // Midterm 40/50 × 30 = 24, Final 75/100 × 50 = 37.5, Project 18/20 × 20 = 18 → 79.5 → 80 = B.
        var final = GradeCalculator.Final(
            [new(50, 30, 40), new(100, 50, 75), new(20, 20, 18)], Scale);

        Assert.Equal(new FinalGrade(80, "B", 3.0m), final);
    }

    [Fact]
    public void Final_59Point5_RoundsTo60_D()
    {
        // 59.5 / 100 × 100 = 59.5 → 60 → D (the boundary from testing.md).
        var final = GradeCalculator.Final([new(100, 100, 59.5m)], Scale);
        Assert.Equal(new FinalGrade(60, "D", 1.0m), final);
    }

    [Fact]
    public void Final_59Point49_StaysF()
    {
        var final = GradeCalculator.Final([new(100, 100, 59.49m)], Scale);
        Assert.Equal(new FinalGrade(59, "F", 0.0m), final);
    }

    [Fact]
    public void Final_MissingScore_CountsAsZero()
    {
        var final = GradeCalculator.Final([new(50, 50, 50), new(100, 50, null)], Scale);
        Assert.Equal(50, final.Pct);
        Assert.Equal("F", final.Letter);
    }

    [Fact]
    public void Final_FullMarks_APlus() =>
        Assert.Equal(new FinalGrade(100, "A+", 4.0m), GradeCalculator.Final([new(20, 40, 20), new(30, 60, 30)], Scale));

    [Fact]
    public void Progress_OnlyGradedComponentsCount()
    {
        // Midterm 40/50 × 30 = 24 earned of 30 graded → running 80%. Final not graded.
        var p = GradeCalculator.Progress([new(50, 30, 40), new(100, 70, null)]);
        Assert.Equal(new GradeProgress(30, 24, 80), p);
    }

    [Fact]
    public void Progress_NothingGraded_RunningIsNull()
    {
        var p = GradeCalculator.Progress([new(50, 30, null)]);
        Assert.Equal(new GradeProgress(0, 0, null), p);
    }

    [Fact]
    public void Progress_RoundsToTwoDecimals()
    {
        // 2/3 × 10 = 6.666… earned of 10 → 66.67%.
        var p = GradeCalculator.Progress([new(3, 10, 2)]);
        Assert.Equal(6.67m, p.Earned);
        Assert.Equal(66.67m, p.RunningPct);
    }
}
