using SmartSis.Api.Features.Enrollments.Enums;

namespace SmartSis.Api.Features.Grading.Models.Dtos;

/// <summary>
/// The student's grades in the current term (PB-09). Also read by the AI assistant with the student's own token, so the shape is
/// kept small and stable.
/// </summary>
/// <param name="TermId">Current term id, or null when no term is current.</param>
/// <param name="TermName">Current term name.</param>
/// <param name="TermGpa">GPA of this term's finalized courses, or null until one is finalized.</param>
/// <param name="CumulativeGpa">GPA over every finalized course (latest attempt of each), or null when there is none yet.</param>
/// <param name="CreditsEarned">Credits passed with D or better, all terms.</param>
/// <param name="Courses">This term's courses (enrolled, completed or dropped for absence), by course code.</param>
public sealed record MyGradesDto(
    int? TermId, string? TermName, decimal? TermGpa, decimal? CumulativeGpa, int CreditsEarned, IReadOnlyList<CourseGradesDto> Courses);

/// <summary>One course of the term with its components.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseCode">Course code, e.g. "CSC 210".</param>
/// <param name="CourseName">Course name.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="Credits">Credits.</param>
/// <param name="Status">"Enrolled" (in progress), "Completed" (final grade below) or "DroppedAbsence" (F for absences).</param>
/// <param name="GradedWeight">Σ weight of the components graded so far (0–100), for the progress bar.</param>
/// <param name="Earned">Points earned so far out of 100: Σ (score / max × weight).</param>
/// <param name="RunningPct">Average on what is graded so far (earned ÷ graded weight × 100), or null when nothing is graded.</param>
/// <param name="FinalPct">Final percent once completed, else null.</param>
/// <param name="LetterGrade">Final letter once completed (F after an absence drop), else null.</param>
/// <param name="GradePoints">0.0 – 4.0 once completed, else null.</param>
/// <param name="Components">The admin's components for the course this term.</param>
public sealed record CourseGradesDto(
    int SectionId, string CourseCode, string CourseName, string SectionCode, int Credits, EnrollmentStatus Status,
    decimal GradedWeight, decimal Earned, decimal? RunningPct, decimal? FinalPct, string? LetterGrade, decimal? GradePoints,
    IReadOnlyList<ComponentGradeDto> Components);

/// <summary>One component and the student's score on it.</summary>
/// <param name="Name">Name, e.g. "Midterm".</param>
/// <param name="Weight">Percent of the final grade.</param>
/// <param name="MaxGrade">Highest possible score.</param>
/// <param name="Score">The student's score, or null when not graded yet ("Not graded").</param>
/// <param name="Pct">Score ÷ max × 100 (2 decimals), or null when not graded.</param>
public sealed record ComponentGradeDto(string Name, decimal Weight, decimal MaxGrade, decimal? Score, decimal? Pct);

/// <summary>The student's academic record across all terms.</summary>
/// <param name="CumulativeGpa">GPA over every finalized course (latest attempt of each), or null when there is none.</param>
/// <param name="CreditsAttempted">Credits of finalized courses (latest attempt of each).</param>
/// <param name="CreditsEarned">Credits passed with D or better (latest attempt of each).</param>
/// <param name="Terms">Terms oldest first.</param>
public sealed record TranscriptDto(decimal? CumulativeGpa, int CreditsAttempted, int CreditsEarned, IReadOnlyList<TranscriptTermDto> Terms);

/// <summary>One term of the transcript. GPA and credits come from TermResults.</summary>
/// <param name="TermId">Term id.</param>
/// <param name="TermName">Term name.</param>
/// <param name="IsCurrent">True for the current term (courses may still be in progress).</param>
/// <param name="TermGpa">GPA of the term's finalized courses, or null when none is finalized.</param>
/// <param name="CumulativeGpa">Cumulative GPA at the end of this term, or null.</param>
/// <param name="CreditsAttempted">Credits of the term's finalized courses.</param>
/// <param name="CreditsEarned">Credits passed in the term.</param>
/// <param name="Courses">Courses by code.</param>
public sealed record TranscriptTermDto(
    int TermId, string TermName, bool IsCurrent, decimal? TermGpa, decimal? CumulativeGpa, int CreditsAttempted, int CreditsEarned,
    IReadOnlyList<TranscriptCourseDto> Courses);

/// <summary>One course on the transcript.</summary>
/// <param name="CourseCode">Course code.</param>
/// <param name="CourseName">Course name.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="Credits">Credits.</param>
/// <param name="Status">"Enrolled" (in progress), "Completed" or "DroppedAbsence".</param>
/// <param name="FinalPct">Final percent, or null.</param>
/// <param name="LetterGrade">Letter, or null while in progress.</param>
/// <param name="GradePoints">0.0 – 4.0, or null while in progress.</param>
/// <param name="CountsInGpa">False for an attempt replaced by a later retake (rule G5).</param>
public sealed record TranscriptCourseDto(
    string CourseCode, string CourseName, string SectionCode, int Credits, EnrollmentStatus Status,
    decimal? FinalPct, string? LetterGrade, decimal? GradePoints, bool CountsInGpa);
