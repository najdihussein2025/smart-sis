using SmartSis.Api.Features.Terms.Enums;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Enrollments.Rules;

/// <summary>
/// Swap an enrolled section for another one (a different course, or another section of the same course), in one step.
/// Allowed in both windows (rule E1). The new section must pass every enroll rule as if the old one were already dropped,
/// so its credits, its course and its meetings no longer count.
/// </summary>
public static class SwapPolicy
{
    public const string SameSection = "swap_same_section";
    public const string NotEnrolled = "swap_not_enrolled";

    public static void Check(StudentEnrollmentState student, int fromSectionId, SectionCandidate to)
    {
        if (student.Window == TermWindow.Closed)
            throw new BusinessRuleException("Registration is closed. You can swap only during the enrollment or add/drop period.",
                EnrollmentPolicy.WindowClosed);

        var from = student.Enrolled.FirstOrDefault(e => e.SectionId == fromSectionId)
                   ?? throw new BusinessRuleException("You can only swap a section you are enrolled in.", NotEnrolled);
        if (from.SectionId == to.SectionId)
            throw new BusinessRuleException($"You're already in {to.Label}. Choose a different section to swap to.", SameSection);

        var without = student with { Enrolled = [.. student.Enrolled.Where(e => e.SectionId != fromSectionId)] };
        EnrollmentPolicy.CheckRest(without, to);
    }
}
