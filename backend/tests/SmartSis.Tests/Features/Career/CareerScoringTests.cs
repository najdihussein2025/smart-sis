using SmartSis.Api.Features.Career.Rules;

namespace SmartSis.Tests.Features.Career;

/// <summary>PB-17 / T65: quiz ~70% + grades ~30% → top 3 careers.</summary>
public sealed class CareerScoringTests
{
    private const int Analytical = 1, Creative = 2, Leadership = 3;

    private static IReadOnlyDictionary<int, decimal> D(params (int Key, decimal Value)[] items) => items.ToDictionary(x => x.Key, x => x.Value);

    private static CareerProfile Career(int id, string name, params (int Trait, int Weight)[] weights) =>
        new(id, name, weights.ToDictionary(w => w.Trait, w => w.Weight));

    [Theory]
    [InlineData(4.0, null, 100.0)]
    [InlineData(3.0, 99.0, 75.0)]   // grade points win over the percentage
    [InlineData(null, 82.0, 82.0)]
    public void GradePercent_FromGradePointsOrPercentage(double? points, double? pct, double expected) =>
        Assert.Equal((decimal)expected, CareerScoring.GradePercent(new CourseGrade(1, (decimal?)points, (decimal?)pct)));

    [Fact]
    public void GradePercent_Ungraded_IsNull() => Assert.Null(CareerScoring.GradePercent(new CourseGrade(1, null, null)));

    [Fact]
    public void GradeTraitScores_AveragesTaggedCourses_SkipsUngradedAndUntagged()
    {
        var tags = new Dictionary<int, IReadOnlyCollection<int>> { [10] = [Analytical], [20] = [Analytical, Creative] };
        var grades = new[]
        {
            new CourseGrade(10, 4.0m, null),   // 100
            new CourseGrade(20, 2.0m, null),   // 50
            new CourseGrade(30, 4.0m, null),   // untagged
            new CourseGrade(10, null, null),   // ungraded
        };
        var scores = CareerScoring.GradeTraitScores(grades, tags);
        Assert.Equal(75m, scores[Analytical]);
        Assert.Equal(50m, scores[Creative]);
        Assert.False(scores.ContainsKey(Leadership));
    }

    [Fact]
    public void TraitScore_CombinesSeventyThirty() =>
        Assert.Equal(0.7m * 80 + 0.3m * 50, CareerScoring.TraitScore(Analytical, D((Analytical, 80)), D((Analytical, 50))));

    [Fact]
    public void TraitScore_NoGrades_UsesQuizAlone() =>
        Assert.Equal(80m, CareerScoring.TraitScore(Analytical, D((Analytical, 80)), D()));

    [Fact]
    public void TraitScore_NotInQuiz_CountsAsZero() =>
        Assert.Equal(0m, CareerScoring.TraitScore(Leadership, D((Analytical, 80)), D()));

    [Fact]
    public void TopMatches_WeightedAverage_BestFirst_TopThree()
    {
        var quiz = D((Analytical, 90), (Creative, 30), (Leadership, 60));
        var careers = new[]
        {
            Career(1, "Data", (Analytical, 10)),                    // 90
            Career(2, "Design", (Creative, 10)),                    // 30
            Career(3, "Manager", (Leadership, 3), (Analytical, 1)), // (180 + 90) / 4 = 67.5 → 68
            Career(4, "Mixed", (Creative, 1), (Leadership, 1)),     // 45
        };

        var top = CareerScoring.TopMatches(careers, quiz, D());

        Assert.Equal(["Data", "Manager", "Mixed"], top.Select(m => m.Name));
        Assert.Equal([90, 68, 45], top.Select(m => m.MatchPct));
        Assert.All(top, m => Assert.Null(m.GradePct));
        Assert.Equal([Leadership, Analytical], top[1].TopTraitIds);
    }

    [Fact]
    public void TopMatches_GradesShiftTheMatch()
    {
        var quiz = D((Analytical, 60), (Creative, 60));
        var grades = D((Analytical, 100));
        var careers = new[] { Career(1, "Data", (Analytical, 1)), Career(2, "Design", (Creative, 1)) };

        var top = CareerScoring.TopMatches(careers, quiz, grades);

        // Data: 0.7 × 60 + 0.3 × 100 = 72; Design keeps the quiz score.
        Assert.Equal(("Data", 72, 60, (int?)100), (top[0].Name, top[0].MatchPct, top[0].QuizPct, top[0].GradePct));
        Assert.Equal(("Design", 60, (int?)null), (top[1].Name, top[1].MatchPct, top[1].GradePct));
    }

    [Fact]
    public void TopMatches_TiesByName_SkipsCareersWithoutWeights()
    {
        var quiz = D((Analytical, 50));
        var careers = new[] { Career(1, "Zeta", (Analytical, 1)), Career(2, "Alpha", (Analytical, 2)), Career(3, "Empty") };
        var top = CareerScoring.TopMatches(careers, quiz, D());
        Assert.Equal(["Alpha", "Zeta"], top.Select(m => m.Name));
    }

    [Fact]
    public void TopMatches_NoCareers_Empty() => Assert.Empty(CareerScoring.TopMatches([], D(), D()));
}
