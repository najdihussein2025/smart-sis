using SmartSis.Api.Features.Sections.Models.Dtos;

namespace SmartSis.Api.Features.Enrollments.Models.Dtos;

/// <summary>The student's enrolled sections in the current term.</summary>
/// <param name="TermId">Current term id, or null when no term is current.</param>
/// <param name="TermName">Current term name.</param>
/// <param name="Window">"Closed", "Enrollment" or "AddDrop". Drop is allowed only in "AddDrop".</param>
/// <param name="EnrolledCredits">Credits enrolled this term.</param>
/// <param name="MaxCredits">The student's credit cap per term.</param>
/// <param name="Items">Enrolled sections, by course code.</param>
public sealed record MyEnrollmentsDto(int? TermId, string? TermName, string Window, int EnrolledCredits, int MaxCredits, IReadOnlyList<EnrolledSectionDto> Items);

/// <summary>An enrolled section.</summary>
/// <param name="EnrollmentId">Enrollment id.</param>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseId">Course id.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="CourseName">Course name.</param>
/// <param name="Credits">Credits.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="InstructorName">Instructor, or null.</param>
/// <param name="Meetings">Weekly meetings.</param>
/// <param name="EnrolledAt">When the student enrolled (UTC).</param>
public sealed record EnrolledSectionDto(
    int EnrollmentId,
    int SectionId,
    int CourseId,
    string CourseCode,
    string CourseName,
    int Credits,
    string SectionCode,
    string? InstructorName,
    IReadOnlyList<SectionMeetingDto> Meetings,
    DateTime EnrolledAt);

/// <summary>Enroll in one or more sections (usually from the cart). Each one is checked and saved on its own.</summary>
/// <param name="SectionIds">1–10 distinct section ids, tried in this order.</param>
public sealed record EnrollRequest(int[] SectionIds);

/// <summary>What happened to each requested section.</summary>
/// <param name="Results">One row per requested section, in request order.</param>
/// <param name="EnrolledCount">How many succeeded.</param>
public sealed record EnrollResultDto(IReadOnlyList<EnrollOutcomeDto> Results, int EnrolledCount);

/// <summary>Outcome for one section.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="Label">E.g. "CSC 210 A", or null when the section doesn't exist.</param>
/// <param name="Success">True when the student is now enrolled.</param>
/// <param name="Message">Why it failed, written for the student (null on success).</param>
/// <param name="Code">Machine code of the broken rule: window_closed, credit_cap, prerequisite_missing, same_course, schedule_conflict, section_full, not_current_term, not_found.</param>
public sealed record EnrollOutcomeDto(int SectionId, string? Label, bool Success, string? Message, string? Code);

/// <summary>Swap an enrolled section for another.</summary>
/// <param name="FromSectionId">A section the student is enrolled in.</param>
/// <param name="ToSectionId">The section to take instead (another course, or another section of the same course).</param>
public sealed record SwapRequest(int FromSectionId, int ToSectionId);

/// <summary>Drop an enrolled section (add/drop period only).</summary>
/// <param name="SectionId">A section the student is enrolled in.</param>
public sealed record DropRequest(int SectionId);
