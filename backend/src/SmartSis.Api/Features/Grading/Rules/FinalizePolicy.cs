using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Grading.Rules;

/// <summary>A score that is still missing, for the message.</summary>
public sealed record MissingScore(string StudentName, string ComponentName);

/// <summary>
/// When a section's grades can be finalized. Pure. Check order: not already finalized → has students → has components →
/// weights add up to exactly 100 → no score missing. Missing scores are never treated as 0: the instructor enters 0 on purpose.
/// </summary>
public static class FinalizePolicy
{
    public const string AlreadyFinalized = "already_finalized";
    public const string NoStudents = "no_students";
    public const string NoComponents = "no_components";
    public const string WeightsIncomplete = "weights_incomplete";
    public const string ScoresMissing = "scores_missing";

    /// <param name="enrolledCount">Students still Enrolled in the section.</param>
    /// <param name="completedCount">Students already Completed in the section.</param>
    /// <param name="weights">Weights of the course's components this term.</param>
    /// <param name="missing">Student × component pairs without a score.</param>
    public static void Check(int enrolledCount, int completedCount, IReadOnlyCollection<decimal> weights, IReadOnlyList<MissingScore> missing)
    {
        if (enrolledCount == 0 && completedCount > 0)
            throw new BusinessRuleException("Grades for this section were already finalized.", AlreadyFinalized);
        if (enrolledCount == 0)
            throw new BusinessRuleException("No students are enrolled in this section, so there is nothing to finalize.", NoStudents);
        if (weights.Count == 0)
            throw new BusinessRuleException("The admin hasn't defined grade components for this course yet.", NoComponents);

        var total = weights.Sum();
        if (total != GradeComponentPolicy.MaxTotalWeight)
            throw new BusinessRuleException(
                $"The grade components add up to {total:0.##}%, not 100%. Ask the admin to complete them before finalizing.", WeightsIncomplete);

        if (missing.Count > 0)
        {
            var first = missing[0];
            var more = missing.Count > 1 ? $" and {missing.Count - 1} more" : "";
            throw new BusinessRuleException(
                $"{missing.Count} score{(missing.Count == 1 ? " is" : "s are")} missing ({first.StudentName}: {first.ComponentName}{more}). " +
                "Enter every score, 0 included, before finalizing.",
                ScoresMissing);
        }
    }
}
