namespace SmartSis.Api.Features.Grading.Rules;

/// <summary>One component of a course with the student's score on it (null = not graded yet).</summary>
public sealed record ComponentScore(decimal MaxGrade, decimal Weight, decimal? Score);

/// <summary>One row of the GradeScales table (PROJECT.md §4).</summary>
public sealed record GradeBand(string Letter, decimal GpaPoints, int MinPct, int MaxPct);

/// <summary>A finished course: rounded percent, letter and grade points.</summary>
public sealed record FinalGrade(int Pct, string Letter, decimal GradePoints);

/// <summary>Where a student stands in a course before it is finalized.</summary>
/// <param name="GradedWeight">Σ weight of the components that have a score.</param>
/// <param name="Earned">Σ (score / max × weight) over the graded components: points earned out of 100 so far.</param>
/// <param name="RunningPct">Earned ÷ graded weight × 100 (the average so far), or null when nothing is graded.</param>
public sealed record GradeProgress(decimal GradedWeight, decimal Earned, decimal? RunningPct);

/// <summary>
/// Rule G3: final % = Σ (score / max × weight), rounded to the nearest whole number (halves away from zero), then looked up in
/// the grade scale. Pure: the caller loads the components, scores and scale.
/// </summary>
public static class GradeCalculator
{
    /// <summary>What one score adds to the course percent.</summary>
    public static decimal Contribution(decimal score, decimal maxGrade, decimal weight) => score / maxGrade * weight;

    /// <summary>Σ (score / max × weight), unrounded. A component without a score adds nothing.</summary>
    public static decimal WeightedPercent(IEnumerable<ComponentScore> components) =>
        components.Where(c => c.Score is not null).Sum(c => Contribution(c.Score!.Value, c.MaxGrade, c.Weight));

    /// <summary>Rule G3 rounding: 59.5 → 60, 59.49 → 59.</summary>
    public static int RoundPercent(decimal pct) => (int)Math.Round(pct, 0, MidpointRounding.AwayFromZero);

    /// <summary>The band whose MinPct ≤ <paramref name="pct"/> ≤ MaxPct.</summary>
    public static GradeBand Lookup(int pct, IReadOnlyList<GradeBand> scale) =>
        scale.FirstOrDefault(b => pct >= b.MinPct && pct <= b.MaxPct)
        ?? throw new InvalidOperationException($"The grade scale has no letter for {pct}%.");

    /// <summary>Final grade from the components. Missing scores count as 0 (the service refuses to finalize with missing scores).</summary>
    public static FinalGrade Final(IEnumerable<ComponentScore> components, IReadOnlyList<GradeBand> scale)
    {
        var pct = RoundPercent(WeightedPercent(components));
        var band = Lookup(pct, scale);
        return new FinalGrade(pct, band.Letter, band.GpaPoints);
    }

    /// <summary>Progress so far, rounded to 2 decimals.</summary>
    public static GradeProgress Progress(IEnumerable<ComponentScore> components)
    {
        var graded = components.Where(c => c.Score is not null).ToList();
        var weight = graded.Sum(c => c.Weight);
        var earned = WeightedPercent(graded);
        decimal? running = weight > 0 ? Round2(earned / weight * 100) : null;
        return new GradeProgress(weight, Round2(earned), running);
    }

    private static decimal Round2(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
}
