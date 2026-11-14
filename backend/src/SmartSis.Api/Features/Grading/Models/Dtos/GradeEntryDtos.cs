using SmartSis.Api.Features.Enrollments.Enums;

namespace SmartSis.Api.Features.Grading.Models.Dtos;

/// <summary>A current-term section the instructor teaches, with how much grading is left.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseCode">Course code, e.g. "CSC 210".</param>
/// <param name="CourseName">Course name.</param>
/// <param name="SectionCode">Section code, e.g. "A".</param>
/// <param name="TermName">Term name.</param>
/// <param name="StudentCount">Students on the roster (enrolled or completed).</param>
/// <param name="ComponentCount">Grade components the admin defined for the course this term.</param>
/// <param name="TotalWeight">Sum of the component weights.</param>
/// <param name="NotGraded">Scores still missing (students × components).</param>
/// <param name="IsFinalized">True once the grades were finalized; scores are then read-only.</param>
public sealed record TeachingSectionDto(
    int SectionId, string CourseCode, string CourseName, string SectionCode, string TermName,
    int StudentCount, int ComponentCount, decimal TotalWeight, int NotGraded, bool IsFinalized);

/// <summary>A section's grading overview: its components and how many students each one still misses.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="CourseName">Course name.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="TermName">Term name.</param>
/// <param name="StudentCount">Students on the roster.</param>
/// <param name="TotalWeight">Sum of the component weights. Must be 100 to finalize.</param>
/// <param name="NotGraded">Scores still missing across all components.</param>
/// <param name="IsFinalized">True once the grades were finalized.</param>
/// <param name="Components">Components, in the order the admin created them.</param>
public sealed record GradingSectionDto(
    int SectionId, string CourseCode, string CourseName, string SectionCode, string TermName,
    int StudentCount, decimal TotalWeight, int NotGraded, bool IsFinalized, IReadOnlyList<GradingComponentDto> Components);

/// <summary>A component as the instructor sees it.</summary>
/// <param name="Id">Component id.</param>
/// <param name="Name">Name, e.g. "Midterm".</param>
/// <param name="MaxGrade">Highest possible score.</param>
/// <param name="Weight">Percent of the final grade.</param>
/// <param name="Graded">Students with a score.</param>
/// <param name="NotGraded">Students without a score ("X not graded").</param>
public sealed record GradingComponentDto(int Id, string Name, decimal MaxGrade, decimal Weight, int Graded, int NotGraded);

/// <summary>The roster for one component, existing scores filled in.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="IsFinalized">True once the grades were finalized; scores can't change.</param>
/// <param name="Component">The component.</param>
/// <param name="Students">Students by name.</param>
public sealed record ComponentRosterDto(
    int SectionId, string CourseCode, string SectionCode, bool IsFinalized, GradingComponentDto Component, IReadOnlyList<RosterStudentDto> Students);

/// <summary>A student on the roster.</summary>
/// <param name="EnrollmentId">Enrollment id (send it back with the score).</param>
/// <param name="UniversityId">University ID.</param>
/// <param name="FullName">Full name.</param>
/// <param name="Status">"Enrolled", or "Completed" after finalizing.</param>
/// <param name="Score">Score on this component, or null when not graded.</param>
public sealed record RosterStudentDto(int EnrollmentId, long UniversityId, string FullName, EnrollmentStatus Status, decimal? Score);

/// <summary>Scores for one component. Students left out keep their score.</summary>
/// <param name="Scores">1–500 entries, one per enrollment.</param>
public sealed record SaveScoresRequest(ScoreEntryRequest[] Scores);

/// <summary>One student's score.</summary>
/// <param name="EnrollmentId">Enrollment id from the roster.</param>
/// <param name="Score">0 … the component's max grade (2 decimals), or null to clear the score.</param>
public sealed record ScoreEntryRequest(int EnrollmentId, decimal? Score);

/// <summary>The result of finalizing a section.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="Students">Final grade of each student, by name.</param>
public sealed record FinalizeResultDto(int SectionId, IReadOnlyList<FinalizedStudentDto> Students);

/// <summary>A student's final grade.</summary>
/// <param name="EnrollmentId">Enrollment id.</param>
/// <param name="UniversityId">University ID.</param>
/// <param name="FullName">Full name.</param>
/// <param name="FinalPct">Final percent, rounded to a whole number.</param>
/// <param name="LetterGrade">Letter from the grade scale.</param>
/// <param name="GradePoints">0.0 – 4.0.</param>
public sealed record FinalizedStudentDto(int EnrollmentId, long UniversityId, string FullName, decimal FinalPct, string LetterGrade, decimal GradePoints);
