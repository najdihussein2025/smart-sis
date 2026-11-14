using SmartSis.Api.Features.Terms.Enums;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Enrollments.Rules;

/// <summary>A weekly meeting time. DayOfWeek: 1 = Monday … 6 = Saturday.</summary>
public sealed record MeetingTime(int DayOfWeek, TimeOnly Start, TimeOnly End);

/// <summary>A prerequisite of the course being enrolled in.</summary>
public sealed record Prerequisite(int CourseId, string Code);

/// <summary>A section the student already holds this term.</summary>
/// <param name="Label">For messages, e.g. "CSC 210 A".</param>
public sealed record HeldSection(int SectionId, int CourseId, string CourseCode, string Label, int Credits, IReadOnlyList<MeetingTime> Meetings);

/// <summary>The section the student wants.</summary>
/// <param name="Label">For messages, e.g. "CSC 310 A".</param>
/// <param name="SeatsTaken">Counted inside the transaction, after the row lock (rule E6).</param>
public sealed record SectionCandidate(
    int SectionId,
    int CourseId,
    string CourseCode,
    string Label,
    int Credits,
    IReadOnlyList<MeetingTime> Meetings,
    IReadOnlyList<Prerequisite> Prerequisites,
    int Capacity,
    int SeatsTaken);

/// <summary>Everything about the student the checks need.</summary>
/// <param name="Window">Window open right now.</param>
/// <param name="MaxCredits">Students.MaxCreditsOverride ?? Majors.DefaultMaxCredits (rule E2).</param>
/// <param name="Enrolled">Sections the student is Enrolled in this term.</param>
/// <param name="PassedCourseIds">Courses with a Completed attempt and GradePoints ≥ 1.0 (rule E3).</param>
public sealed record StudentEnrollmentState(TermWindow Window, int MaxCredits, IReadOnlyList<HeldSection> Enrolled, IReadOnlySet<int> PassedCourseIds);

/// <summary>
/// Rules E1–E6 for enroll (and, through <see cref="SwapPolicy"/>, swap). Pure: the caller gathers the facts.
/// Check order: window → credit cap → prerequisites → same course → time conflict → capacity. The first failure throws a
/// <see cref="BusinessRuleException"/> with a message written for the student and a machine code for the frontend.
/// </summary>
public static class EnrollmentPolicy
{
    private static readonly string[] Days = ["", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public const string WindowClosed = "window_closed";
    public const string CreditCap = "credit_cap";
    public const string PrerequisiteMissing = "prerequisite_missing";
    public const string SameCourse = "same_course";
    public const string ScheduleConflict = "schedule_conflict";
    public const string SectionFull = "section_full";
    public const string DropWindowClosed = "drop_window_closed";

    /// <summary>Enroll (add) is allowed in both the enrollment and the add/drop window (rule E1).</summary>
    public static void CheckEnroll(StudentEnrollmentState student, SectionCandidate section)
    {
        if (student.Window == TermWindow.Closed)
            throw new BusinessRuleException("Registration is closed. You can enroll only during the enrollment or add/drop period.", WindowClosed);
        CheckRest(student, section);
    }

    /// <summary>Everything after the window check. Used by <see cref="SwapPolicy"/> with the swapped-out section removed.</summary>
    internal static void CheckRest(StudentEnrollmentState student, SectionCandidate section)
    {
        var credits = student.Enrolled.Sum(e => e.Credits) + section.Credits;
        if (credits > student.MaxCredits)
            throw new BusinessRuleException(
                $"Adding {section.CourseCode} would bring you to {credits} credits; your limit is {student.MaxCredits} per term.", CreditCap);

        var missing = section.Prerequisites.Where(p => !student.PassedCourseIds.Contains(p.CourseId)).Select(p => p.Code).ToList();
        if (missing.Count > 0)
            throw new BusinessRuleException(
                $"You need to pass {string.Join(" and ", missing)} (D or better) before {section.CourseCode}.", PrerequisiteMissing);

        var same = student.Enrolled.FirstOrDefault(e => e.CourseId == section.CourseId);
        if (same is not null)
            throw new BusinessRuleException(
                same.SectionId == section.SectionId
                    ? $"You're already enrolled in {section.Label}."
                    : $"You're already enrolled in {same.Label}. Swap it instead of taking a second section.",
                SameCourse);

        foreach (var held in student.Enrolled)
            foreach (var mine in section.Meetings)
            {
                var clash = held.Meetings.FirstOrDefault(m => Overlaps(mine, m));
                if (clash is not null)
                    throw new BusinessRuleException(
                        $"Schedule conflict with {held.Label} on {Days[clash.DayOfWeek]} {clash.Start:HH\\:mm}–{clash.End:HH\\:mm}.",
                        ScheduleConflict);
            }

        if (section.SeatsTaken >= section.Capacity)
            throw new BusinessRuleException($"Section {section.Label} is full.", SectionFull);
    }

    /// <summary>Dropping is allowed only in the add/drop window (rule E1).</summary>
    public static void CheckDrop(TermWindow window)
    {
        if (window != TermWindow.AddDrop)
            throw new BusinessRuleException("Courses can be dropped only during the add/drop period.", DropWindowClosed);
    }

    /// <summary>Rule E5: same day and <c>start &lt; otherEnd &amp;&amp; otherStart &lt; end</c>. Back-to-back is fine.</summary>
    public static bool Overlaps(MeetingTime a, MeetingTime b) => a.DayOfWeek == b.DayOfWeek && a.Start < b.End && b.Start < a.End;
}
