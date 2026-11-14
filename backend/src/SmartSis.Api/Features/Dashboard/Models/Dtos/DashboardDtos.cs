namespace SmartSis.Api.Features.Dashboard.Models.Dtos;

/// <summary>The numbers on the student dashboard (PB-22). Read-only; every value comes from the owning feature.</summary>
/// <param name="TermName">Current term name, or null when no term is current.</param>
/// <param name="CumulativeGpa">GPA over every finalized course (latest attempt of each), or null when there is none yet.</param>
/// <param name="TermGpa">GPA of this term's finalized courses, or null until one is finalized.</param>
/// <param name="CreditsEarned">Credits passed with D or better, all terms.</param>
/// <param name="EnrolledCredits">Credits of the sections the student is Enrolled in this term.</param>
/// <param name="MaxCredits">The student's credit cap (rule E2).</param>
/// <param name="ClassesToday">Meetings of the student's enrolled sections on today's weekday.</param>
/// <param name="OutstandingBalance">What the student still owes over all charges.</param>
/// <param name="OverdueBalance">The part of the balance whose due date has passed.</param>
/// <param name="NextDueDate">Earliest due date of a charge that isn't fully paid.</param>
/// <param name="UnreadNotifications">Unread in-app notifications.</param>
/// <param name="AttendanceWarnings">This term's enrolled courses whose standing is Warning or Critical (rule A5).</param>
/// <param name="AttendanceCritical">This term's enrolled courses whose standing is Critical (rule A5).</param>
public sealed record StudentDashboardDto(
    string? TermName,
    decimal? CumulativeGpa,
    decimal? TermGpa,
    int CreditsEarned,
    int EnrolledCredits,
    int MaxCredits,
    int ClassesToday,
    decimal OutstandingBalance,
    decimal OverdueBalance,
    DateOnly? NextDueDate,
    int UnreadNotifications,
    int AttendanceWarnings,
    int AttendanceCritical);

/// <summary>The numbers on the instructor dashboard (PB-22), for the sections the signed-in instructor teaches this term.</summary>
/// <param name="TermName">Current term name, or null when no term is current.</param>
/// <param name="Today">The date the counts are for (UTC, like attendance).</param>
/// <param name="ActiveSections">Sections taught this term.</param>
/// <param name="ClassesToday">Meetings of those sections on today's weekday.</param>
/// <param name="MissingScores">Scores not entered yet: enrolled students × components, minus scores entered (sections not finalized).</param>
/// <param name="SectionsWithMissingScores">Sections with at least one missing score.</param>
/// <param name="AttendanceToTake">Meetings from Monday of this week up to today (inside the term) with no attendance session yet.</param>
public sealed record InstructorDashboardDto(
    string? TermName,
    DateOnly Today,
    int ActiveSections,
    int ClassesToday,
    int MissingScores,
    int SectionsWithMissingScores,
    int AttendanceToTake);

/// <summary>The numbers on the admin dashboard (PB-22).</summary>
/// <param name="TermName">Current term name, or null when no term is current.</param>
/// <param name="ActiveStudents">Students that can sign in.</param>
/// <param name="ActiveInstructors">Instructors that can sign in.</param>
/// <param name="SectionsThisTerm">Sections of the current term (0 when no term is current).</param>
/// <param name="OpenSupportRequests">Support requests that are Open or In progress.</param>
/// <param name="NewSupportRequests">Support requests still Open (nobody has picked them up).</param>
/// <param name="UnpaidCharges">Charges that are Pending or Partial (rule F2).</param>
/// <param name="OutstandingTotal">What students still owe over those charges.</param>
public sealed record AdminDashboardDto(
    string? TermName,
    int ActiveStudents,
    int ActiveInstructors,
    int SectionsThisTerm,
    int OpenSupportRequests,
    int NewSupportRequests,
    int UnpaidCharges,
    decimal OutstandingTotal);
