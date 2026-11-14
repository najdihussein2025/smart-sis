using SmartSis.Api.Features.Career.Rules;

namespace SmartSis.Api.Features.Career.Models.Dtos;

/// <summary>The signed-in student's assessments and whether they can take one now.</summary>
/// <param name="CurrentTermId">Current term, or null.</param>
/// <param name="CurrentTermName">Current term name, or null.</param>
/// <param name="CanTake">True when the student may take the assessment now (one per term).</param>
/// <param name="Reason">Why <paramref name="CanTake"/> is false, written for the student; null when they can take it.</param>
/// <param name="Attempts">Submitted assessments, newest first.</param>
public sealed record MyAssessmentsDto(int? CurrentTermId, string? CurrentTermName, bool CanTake, string? Reason, IReadOnlyList<AssessmentDto> Attempts);

/// <summary>A submitted assessment with its trait scores.</summary>
/// <param name="Id">Assessment id.</param>
/// <param name="TermId">Term it was taken in.</param>
/// <param name="TermName">Term name.</param>
/// <param name="SubmittedAt">When it was submitted (UTC).</param>
/// <param name="Traits">Trait scores, highest first.</param>
public sealed record AssessmentDto(int Id, int TermId, string TermName, DateTime SubmittedAt, IReadOnlyList<TraitScoreDto> Traits);

/// <summary>Quiz result for one trait.</summary>
/// <param name="TraitId">Trait.</param>
/// <param name="TraitName">Trait name.</param>
/// <param name="Points">Points earned.</param>
/// <param name="MaxPoints">Most points possible.</param>
/// <param name="Score">0–100.</param>
public sealed record TraitScoreDto(int TraitId, string TraitName, int Points, int MaxPoints, int Score);

/// <summary>The quiz: 20–30 active questions. Trait points are not sent.</summary>
/// <param name="TermName">Term the attempt will count for.</param>
/// <param name="Questions">Questions in order.</param>
public sealed record QuizDto(string TermName, IReadOnlyList<QuizQuestionDto> Questions);

/// <summary>A quiz question.</summary>
public sealed record QuizQuestionDto(int Id, string Text, IReadOnlyList<QuizOptionDto> Options);

/// <summary>A quiz answer option.</summary>
public sealed record QuizOptionDto(int Id, string Text);

/// <summary>Submit the quiz: every question once.</summary>
/// <param name="Answers">One answer per quiz question.</param>
public sealed record SubmitAssessmentRequest(AnswerRequest[] Answers);

/// <summary>The chosen option of a question.</summary>
public sealed record AnswerRequest(int QuestionId, int OptionId);

/// <summary>Who wrote the explanation.</summary>
public enum ExplanationSource
{
    /// <summary>Written by the LLM through the AI service.</summary>
    Ai,
    /// <summary>The built-in explanation, used when the AI service isn't available.</summary>
    Fallback,
}

/// <summary>Top 3 careers for the student's latest assessment (PB-17).</summary>
/// <param name="AssessmentId">Assessment the scores come from.</param>
/// <param name="TermName">Term it was taken in.</param>
/// <param name="SubmittedAt">When it was submitted (UTC).</param>
/// <param name="UsesGrades">True when grades in trait-tagged courses were part of the score.</param>
/// <param name="Careers">Up to 3 careers, best first.</param>
/// <param name="Explanation">Why these careers fit (LLM-written, or the built-in text).</param>
/// <param name="ExplanationSource">"Ai" or "Fallback".</param>
/// <param name="SuggestedCourses">Not-yet-passed courses from these careers' roadmaps, with this term's sections.</param>
public sealed record RecommendationsDto(
    int AssessmentId,
    string TermName,
    DateTime SubmittedAt,
    bool UsesGrades,
    IReadOnlyList<CareerMatchDto> Careers,
    string Explanation,
    ExplanationSource ExplanationSource,
    IReadOnlyList<SuggestedCourseDto> SuggestedCourses);

/// <summary>A recommended career.</summary>
/// <param name="CareerId">Career id (use it for the roadmap).</param>
/// <param name="Name">Career name.</param>
/// <param name="Description">Description, or null.</param>
/// <param name="MatchPct">Match 0–100.</param>
/// <param name="QuizPct">Match from the quiz alone.</param>
/// <param name="GradePct">Match from grades alone, or null when no related course is graded.</param>
/// <param name="TopTraits">Traits that contributed most.</param>
public sealed record CareerMatchDto(int CareerId, string Name, string? Description, int MatchPct, int QuizPct, int? GradePct, IReadOnlyList<string> TopTraits);

/// <summary>A course worth taking for the recommended careers.</summary>
/// <param name="CourseId">Course id.</param>
/// <param name="Code">Course code.</param>
/// <param name="Name">Course name.</param>
/// <param name="Credits">Credits.</param>
/// <param name="Careers">Recommended careers whose roadmap includes it.</param>
/// <param name="Sections">This term's sections (empty when not offered).</param>
public sealed record SuggestedCourseDto(int CourseId, string Code, string Name, int Credits, IReadOnlyList<string> Careers, IReadOnlyList<CareerSectionDto> Sections);

/// <summary>A section of the current term, for "Add to cart".</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="SeatsLeft">Free seats.</param>
/// <param name="Status">"Available", "InCart" or "Enrolled".</param>
public sealed record CareerSectionDto(int SectionId, string SectionCode, int SeatsLeft, string Status);

/// <summary>An active career, for choosing a roadmap.</summary>
public sealed record CareerSummaryDto(int Id, string Name, string? Description);

/// <summary>A career's roadmap personalized for the student (PB-20).</summary>
/// <param name="CareerId">Career id.</param>
/// <param name="Name">Career name.</param>
/// <param name="Description">Description, or null.</param>
/// <param name="DoneCount">Course steps already passed.</param>
/// <param name="CourseStepCount">Steps linked to a course.</param>
/// <param name="Steps">Steps in order.</param>
public sealed record RoadmapDto(int CareerId, string Name, string? Description, int DoneCount, int CourseStepCount, IReadOnlyList<RoadmapStepViewDto> Steps);

/// <summary>A roadmap step with the student's status.</summary>
/// <param name="StepOrder">1-based position.</param>
/// <param name="Title">Title.</param>
/// <param name="Description">Description, or null.</param>
/// <param name="Status">Done, InProgress, Next, Later or Milestone (no course).</param>
/// <param name="Course">Linked course with this term's sections, or null.</param>
public sealed record RoadmapStepViewDto(int StepOrder, string Title, string? Description, RoadmapStepStatus Status, RoadmapCourseDto? Course);

/// <summary>A course linked to a roadmap step.</summary>
public sealed record RoadmapCourseDto(int Id, string Code, string Name, int Credits, IReadOnlyList<CareerSectionDto> Sections);
