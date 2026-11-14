using FluentValidation;
using SmartSis.Api.Features.Career.Rules;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Career;

/// <summary>PB-16 / T27–T28: quiz size, answer checks and trait scores.</summary>
public sealed class AssessmentScoringTests
{
    private const int Analytical = 1, Creative = 2;

    /// <summary>Q1: A → analytical 3, B → creative 2. Q2: A → analytical 1 + creative 1, B → creative 3.</summary>
    private static readonly IReadOnlyList<QuizQuestion> Quiz =
    [
        new(1, [new(11, new Dictionary<int, int> { [Analytical] = 3 }), new(12, new Dictionary<int, int> { [Creative] = 2 })]),
        new(2, [new(21, new Dictionary<int, int> { [Analytical] = 1, [Creative] = 1 }), new(22, new Dictionary<int, int> { [Creative] = 3 })]),
    ];

    [Theory]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(30, true)]
    public void PickQuiz_Boundaries(int active, bool ok)
    {
        var ids = Enumerable.Range(1, active).ToList();
        if (ok) Assert.Equal(active, AssessmentScoring.PickQuiz(ids, x => x).Count);
        else Assert.Equal(AssessmentScoring.NotEnoughQuestions,
            Assert.Throws<BusinessRuleException>(() => AssessmentScoring.PickQuiz(ids, x => x)).Code);
    }

    [Fact]
    public void PickQuiz_MoreThan30_TakesTheLowest30Ids()
    {
        var picked = AssessmentScoring.PickQuiz(Enumerable.Range(1, 35).Reverse(), x => x);
        Assert.Equal(Enumerable.Range(1, 30), picked);
    }

    [Fact]
    public void CheckAnswers_AllAnswered_ReturnsMap()
    {
        var map = AssessmentScoring.CheckAnswers(Quiz, [new(2, 22), new(1, 11)]);
        Assert.Equal(11, map[1]);
        Assert.Equal(22, map[2]);
    }

    [Fact]
    public void CheckAnswers_QuestionNotInQuiz_QuizChanged() =>
        Assert.Equal(AssessmentScoring.QuizChanged,
            Assert.Throws<BusinessRuleException>(() => AssessmentScoring.CheckAnswers(Quiz, [new(1, 11), new(2, 21), new(99, 1)])).Code);

    [Fact]
    public void CheckAnswers_OptionOfAnotherQuestion_400OnTheField()
    {
        var ex = Assert.Throws<ValidationException>(() => AssessmentScoring.CheckAnswers(Quiz, [new(1, 11), new(2, 12)]));
        Assert.Equal("answers[1].optionId", Assert.Single(ex.Errors).PropertyName);
    }

    [Fact]
    public void CheckAnswers_SameQuestionTwice_400()
    {
        var ex = Assert.Throws<ValidationException>(() => AssessmentScoring.CheckAnswers(Quiz, [new(1, 11), new(1, 12), new(2, 21)]));
        Assert.Equal("answers[1].questionId", Assert.Single(ex.Errors).PropertyName);
    }

    [Fact]
    public void CheckAnswers_QuestionLeftOut_QuizIncomplete()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => AssessmentScoring.CheckAnswers(Quiz, [new(1, 11)]));
        Assert.Equal(AssessmentScoring.QuizIncomplete, ex.Code);
        Assert.Contains("1 is still unanswered", ex.Message);
    }

    [Fact]
    public void Score_UsesBestOptionPerQuestionAsMax()
    {
        // Max analytical = 3 (Q1) + 1 (Q2) = 4; max creative = 2 + 3 = 5.
        var scores = AssessmentScoring.Score(Quiz, new Dictionary<int, int> { [1] = 11, [2] = 21 });
        Assert.Equal([new TraitScore(Analytical, 4, 4, 100), new TraitScore(Creative, 1, 5, 20)], scores);
    }

    [Fact]
    public void Score_RoundsHalfAwayFromZero()
    {
        // Creative 1 of 8 = 12.5% → 13.
        IReadOnlyList<QuizQuestion> quiz =
        [
            new(1, [new(1, new Dictionary<int, int> { [Creative] = 1 }), new(2, new Dictionary<int, int> { [Creative] = 4 })]),
            new(2, [new(3, new Dictionary<int, int>()), new(4, new Dictionary<int, int> { [Creative] = 4 })]),
        ];
        var s = Assert.Single(AssessmentScoring.Score(quiz, new Dictionary<int, int> { [1] = 1, [2] = 3 }));
        Assert.Equal(13, s.Score);
    }
}
