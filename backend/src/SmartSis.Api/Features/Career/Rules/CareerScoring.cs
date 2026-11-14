namespace SmartSis.Api.Features.Career.Rules;

/// <summary>A completed course on the student's record.</summary>
/// <param name="CourseId">Course.</param>
/// <param name="GradePoints">0.0–4.0, or null when only a percentage is known.</param>
/// <param name="FinalPct">0–100, or null.</param>
public sealed record CourseGrade(int CourseId, decimal? GradePoints, decimal? FinalPct);

/// <summary>A career and how much each trait matters for it (traitId → weight 1–10).</summary>
public sealed record CareerProfile(int CareerId, string Name, IReadOnlyDictionary<int, int> Weights);

/// <summary>How well a career fits the student.</summary>
/// <param name="CareerId">Career.</param>
/// <param name="Name">Career name.</param>
/// <param name="MatchPct">Final match 0–100 (quiz ~70% + grades ~30%).</param>
/// <param name="QuizPct">Match from the quiz alone.</param>
/// <param name="GradePct">Match from grades in trait-tagged courses, or null when none of the career's traits has a graded course.</param>
/// <param name="TopTraitIds">The career's traits that contributed most, strongest first (at most 3).</param>
public sealed record CareerMatch(int CareerId, string Name, int MatchPct, int QuizPct, int? GradePct, IReadOnlyList<int> TopTraitIds);

/// <summary>
/// PB-17 / T65: rule-based career matching. Pure: no database, no clock.
/// <list type="number">
/// <item>Quiz score per trait (0–100) comes from the assessment.</item>
/// <item>Grade score per trait = average grade (GradePoints / 4 × 100, or FinalPct when there are no grade points)
/// over the completed courses tagged with the trait.</item>
/// <item>Trait score = 70% quiz + 30% grades. A trait with no graded course uses the quiz alone.</item>
/// <item>Career match = trait scores averaged with the career's weights, rounded to a whole percent.</item>
/// </list>
/// </summary>
public static class CareerScoring
{
    public const decimal QuizShare = 0.7m;
    public const decimal GradeShare = 0.3m;
    public const int TopCount = 3;

    /// <summary>A grade as a 0–100 percentage, or null when the course has neither grade points nor a percentage.</summary>
    public static decimal? GradePercent(CourseGrade g) =>
        g.GradePoints is { } gp ? Math.Clamp(gp, 0m, 4m) / 4m * 100m
        : g.FinalPct is { } pct ? Math.Clamp(pct, 0m, 100m)
        : null;

    /// <summary>traitId → average grade percentage over the completed courses tagged with that trait.</summary>
    public static IReadOnlyDictionary<int, decimal> GradeTraitScores(
        IEnumerable<CourseGrade> completed, IReadOnlyDictionary<int, IReadOnlyCollection<int>> courseTraits)
    {
        var sums = new Dictionary<int, (decimal Sum, int Count)>();
        foreach (var g in completed)
        {
            if (GradePercent(g) is not { } pct || !courseTraits.TryGetValue(g.CourseId, out var traits)) continue;
            foreach (var t in traits)
            {
                var (sum, count) = sums.GetValueOrDefault(t);
                sums[t] = (sum + pct, count + 1);
            }
        }
        return sums.ToDictionary(s => s.Key, s => s.Value.Sum / s.Value.Count);
    }

    /// <summary>70% quiz + 30% grades; the quiz alone for a trait without graded courses. A missing quiz score counts as 0.</summary>
    public static decimal TraitScore(int traitId, IReadOnlyDictionary<int, decimal> quiz, IReadOnlyDictionary<int, decimal> grades)
    {
        var q = quiz.GetValueOrDefault(traitId);
        return grades.TryGetValue(traitId, out var g) ? QuizShare * q + GradeShare * g : q;
    }

    /// <summary>The best <paramref name="take"/> careers, highest match first; ties by name. Careers without weights are skipped.</summary>
    public static IReadOnlyList<CareerMatch> TopMatches(
        IEnumerable<CareerProfile> careers,
        IReadOnlyDictionary<int, decimal> quiz,
        IReadOnlyDictionary<int, decimal> grades,
        int take = TopCount) =>
        careers
            .Where(c => c.Weights.Values.Sum() > 0)
            .Select(c => Match(c, quiz, grades))
            .OrderByDescending(m => m.Exact)
            .ThenBy(m => m.Result.Name, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(m => m.Result)
            .ToList();

    private static (decimal Exact, CareerMatch Result) Match(
        CareerProfile c, IReadOnlyDictionary<int, decimal> quiz, IReadOnlyDictionary<int, decimal> grades)
    {
        decimal totalWeight = c.Weights.Values.Sum();
        var combined = c.Weights.Sum(w => w.Value * TraitScore(w.Key, quiz, grades)) / totalWeight;
        var quizOnly = c.Weights.Sum(w => w.Value * quiz.GetValueOrDefault(w.Key)) / totalWeight;

        var graded = c.Weights.Where(w => grades.ContainsKey(w.Key)).ToList();
        int? gradePct = graded.Count == 0
            ? null
            : Round(graded.Sum(w => w.Value * grades[w.Key]) / graded.Sum(w => w.Value));

        var top = c.Weights
            .Select(w => (TraitId: w.Key, Contribution: w.Value * TraitScore(w.Key, quiz, grades)))
            .Where(x => x.Contribution > 0)
            .OrderByDescending(x => x.Contribution).ThenBy(x => x.TraitId)
            .Take(3).Select(x => x.TraitId).ToList();

        return (combined, new CareerMatch(c.CareerId, c.Name, Round(combined), Round(quizOnly), gradePct, top));
    }

    private static int Round(decimal v) => (int)Math.Round(Math.Clamp(v, 0m, 100m), MidpointRounding.AwayFromZero);
}
