using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Career.Rules;

/// <summary>An option of a quiz question and the points it adds per trait (traitId → points).</summary>
public sealed record QuizOption(int Id, IReadOnlyDictionary<int, int> TraitPoints);

/// <summary>A question in the quiz with its options.</summary>
public sealed record QuizQuestion(int Id, IReadOnlyList<QuizOption> Options);

/// <summary>One answer as the student sent it.</summary>
public sealed record QuizAnswer(int QuestionId, int OptionId);

/// <summary>Quiz result for one trait.</summary>
/// <param name="TraitId">Trait.</param>
/// <param name="Points">Points earned.</param>
/// <param name="MaxPoints">Most points the quiz could give this trait (best option of every question).</param>
/// <param name="Score">Points / MaxPoints as a whole percentage, rounded half away from zero.</param>
public sealed record TraitScore(int TraitId, int Points, int MaxPoints, int Score);

/// <summary>
/// PB-16 rules for one assessment: which questions make up the quiz, that the answers fit the quiz, and the trait scores.
/// Pure: no database.
/// </summary>
public static class AssessmentScoring
{
    public const int MinQuestions = 20;
    public const int MaxQuestions = 30;

    public const string NotEnoughQuestions = "not_enough_questions";
    public const string QuizChanged = "quiz_changed";
    public const string QuizIncomplete = "quiz_incomplete";

    /// <summary>
    /// The quiz is the first <see cref="MaxQuestions"/> active questions by id, so the same set comes back on submit.
    /// Fewer than <see cref="MinQuestions"/> active questions → 409 (the admin must add more).
    /// </summary>
    public static IReadOnlyList<T> PickQuiz<T>(IEnumerable<T> activeQuestions, Func<T, int> id)
    {
        var picked = activeQuestions.OrderBy(id).Take(MaxQuestions).ToList();
        if (picked.Count < MinQuestions)
            throw new BusinessRuleException(
                $"The career assessment isn't ready yet: it needs at least {MinQuestions} questions. Please try again later.",
                NotEnoughQuestions);
        return picked;
    }

    /// <summary>
    /// Every quiz question answered exactly once, with an option of that question. Returns questionId → optionId.
    /// Check order: a question not in the quiz (409) → answered twice or an option of another question (400)
    /// → a question left out (409).
    /// </summary>
    public static IReadOnlyDictionary<int, int> CheckAnswers(IReadOnlyList<QuizQuestion> quiz, IReadOnlyList<QuizAnswer> answers)
    {
        var byId = quiz.ToDictionary(q => q.Id);
        if (answers.Any(a => !byId.ContainsKey(a.QuestionId)))
            throw new BusinessRuleException("The assessment changed while you were answering. Reload it and try again.", QuizChanged);

        var failures = new List<ValidationFailure>();
        var seen = new HashSet<int>();
        for (var i = 0; i < answers.Count; i++)
        {
            var a = answers[i];
            if (!seen.Add(a.QuestionId))
                failures.Add(new($"answers[{i}].questionId", "This question is answered twice."));
            else if (byId[a.QuestionId].Options.All(o => o.Id != a.OptionId))
                failures.Add(new($"answers[{i}].optionId", "Choose one of this question's options."));
        }
        if (failures.Count > 0) throw new ValidationException(failures);

        var missing = quiz.Count(q => !seen.Contains(q.Id));
        if (missing > 0)
            throw new BusinessRuleException(
                missing == 1 ? "Answer every question: 1 is still unanswered." : $"Answer every question: {missing} are still unanswered.",
                QuizIncomplete);

        return answers.ToDictionary(a => a.QuestionId, a => a.OptionId);
    }

    /// <summary>Trait scores for the chosen options. Traits the quiz can't score (max 0) are left out.</summary>
    public static IReadOnlyList<TraitScore> Score(IReadOnlyList<QuizQuestion> quiz, IReadOnlyDictionary<int, int> answers)
    {
        var points = new Dictionary<int, int>();
        var max = new Dictionary<int, int>();
        foreach (var q in quiz)
        {
            foreach (var traitId in q.Options.SelectMany(o => o.TraitPoints.Keys).Distinct())
                max[traitId] = max.GetValueOrDefault(traitId) + q.Options.Max(o => o.TraitPoints.GetValueOrDefault(traitId));

            if (!answers.TryGetValue(q.Id, out var optionId)) continue;
            var chosen = q.Options.First(o => o.Id == optionId);
            foreach (var (traitId, p) in chosen.TraitPoints)
                points[traitId] = points.GetValueOrDefault(traitId) + p;
        }

        return max.Where(m => m.Value > 0)
            .OrderBy(m => m.Key)
            .Select(m =>
            {
                var earned = points.GetValueOrDefault(m.Key);
                return new TraitScore(m.Key, earned, m.Value, Percent(earned, m.Value));
            })
            .ToList();
    }

    internal static int Percent(decimal part, decimal whole) =>
        whole <= 0 ? 0 : (int)Math.Round(part * 100m / whole, MidpointRounding.AwayFromZero);
}
