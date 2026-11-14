namespace SmartSis.Api.Features.Grading.Rules;

/// <summary>
/// One graded attempt: a Completed enrollment, or a DroppedAbsence one (F, 0.0, rule A3). Plain Dropped rows are never passed in.
/// </summary>
/// <param name="TermId">Term of the attempt.</param>
/// <param name="CourseId">Course, to find retakes.</param>
/// <param name="Credits">Course credits.</param>
/// <param name="GradePoints">0.0 – 4.0.</param>
public sealed record GradedAttempt(int TermId, int CourseId, int Credits, decimal GradePoints)
{
    /// <summary>D or better earns the credits (rule E3).</summary>
    public bool Passed => GradePoints >= 1.0m;
}

/// <summary>One TermResults row.</summary>
/// <param name="TermId">Term.</param>
/// <param name="TermGpa">GPA of the attempts graded in this term.</param>
/// <param name="CumulativeGpa">GPA of everything up to and including this term, latest attempt per course only.</param>
/// <param name="CreditsAttempted">Credits of the attempts graded in this term.</param>
/// <param name="CreditsEarned">Credits passed (D or better) in this term.</param>
public sealed record TermGpaResult(int TermId, decimal TermGpa, decimal CumulativeGpa, int CreditsAttempted, int CreditsEarned);

/// <summary>Cumulative totals over the latest attempt of each course.</summary>
public sealed record CumulativeGpa(decimal Gpa, int CreditsAttempted, int CreditsEarned);

/// <summary>
/// Rule G4: GPA on the 4.0 scale, weighted by credits, rounded to 2 decimals.
/// Rule G5: when a course was taken more than once, only the latest attempt counts in the cumulative GPA.
/// A term GPA shows that term's own grades. Pure.
/// </summary>
public static class GpaCalculator
{
    /// <summary>Σ (points × credits) ÷ Σ credits, 2 decimals (halves away from zero). 0 when there are no credits.</summary>
    public static decimal Gpa(IEnumerable<GradedAttempt> attempts)
    {
        var list = attempts.ToList();
        var credits = list.Sum(a => a.Credits);
        if (credits == 0) return 0m;
        return Math.Round(list.Sum(a => a.GradePoints * a.Credits) / credits, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Keeps the latest attempt of each course. <paramref name="termOrder"/> lists term ids oldest first; attempts in the same
    /// term keep their input order (the last one wins).
    /// </summary>
    public static List<GradedAttempt> LatestPerCourse(IEnumerable<GradedAttempt> attempts, IReadOnlyList<int> termOrder) =>
        attempts
            .Select((a, i) => (a, i))
            .OrderBy(x => IndexOf(termOrder, x.a.TermId)).ThenBy(x => x.i)
            .GroupBy(x => x.a.CourseId)
            .Select(g => g.Last().a)
            .ToList();

    public static CumulativeGpa Cumulative(IEnumerable<GradedAttempt> attempts, IReadOnlyList<int> termOrder)
    {
        var latest = LatestPerCourse(attempts, termOrder);
        return new CumulativeGpa(Gpa(latest), latest.Sum(a => a.Credits), latest.Where(a => a.Passed).Sum(a => a.Credits));
    }

    /// <summary>
    /// The TermResults rows (rule G6) for one student: one per term that has a graded attempt, oldest first.
    /// The cumulative GPA of a term only looks at that term and the ones before it.
    /// </summary>
    public static List<TermGpaResult> ByTerm(IEnumerable<GradedAttempt> attempts, IReadOnlyList<int> termOrder)
    {
        var all = attempts.ToList();
        var results = new List<TermGpaResult>();
        for (var i = 0; i < termOrder.Count; i++)
        {
            var termId = termOrder[i];
            var inTerm = all.Where(a => a.TermId == termId).ToList();
            if (inTerm.Count == 0) continue;

            var upTo = termOrder.Take(i + 1).ToHashSet();
            var cumulative = Cumulative(all.Where(a => upTo.Contains(a.TermId)), termOrder);
            results.Add(new TermGpaResult(
                termId, Gpa(inTerm), cumulative.Gpa, inTerm.Sum(a => a.Credits), inTerm.Where(a => a.Passed).Sum(a => a.Credits)));
        }
        return results;
    }

    private static int IndexOf(IReadOnlyList<int> order, int termId)
    {
        for (var i = 0; i < order.Count; i++)
            if (order[i] == termId) return i;
        return int.MaxValue;
    }
}
