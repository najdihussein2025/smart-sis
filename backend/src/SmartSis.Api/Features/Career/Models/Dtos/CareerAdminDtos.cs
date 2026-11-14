namespace SmartSis.Api.Features.Career.Models.Dtos;

/// <summary>A trait the quiz measures and careers weigh.</summary>
/// <param name="Id">Trait id.</param>
/// <param name="Name">Name, e.g. "Analytical thinking".</param>
/// <param name="Description">Short description, or null.</param>
public sealed record TraitDto(int Id, string Name, string? Description);

/// <summary>Create or rename a trait.</summary>
/// <param name="Name">Unique name (2–60 characters).</param>
/// <param name="Description">Optional, up to 300 characters.</param>
public sealed record SaveTraitRequest(string Name, string? Description);

/// <summary>The whole question bank with what the quiz needs.</summary>
/// <param name="ActiveCount">Active questions. The quiz needs at least <paramref name="MinQuestions"/>.</param>
/// <param name="MinQuestions">Fewest active questions for students to take the quiz (20).</param>
/// <param name="MaxQuestions">Most questions in one quiz (30); extra active questions are left out, lowest ids first in.</param>
/// <param name="Questions">All questions, active first, then by id.</param>
public sealed record QuestionBankDto(int ActiveCount, int MinQuestions, int MaxQuestions, IReadOnlyList<QuestionDto> Questions);

/// <summary>A question of the bank.</summary>
/// <param name="Id">Question id.</param>
/// <param name="Text">Question text.</param>
/// <param name="IsActive">Inactive questions stay in history but aren't asked.</param>
/// <param name="AnswerCount">Times it was answered. Answered questions can be deactivated but not deleted.</param>
/// <param name="Options">Answer options in order.</param>
public sealed record QuestionDto(int Id, string Text, bool IsActive, int AnswerCount, IReadOnlyList<QuestionOptionDto> Options);

/// <summary>An answer option and the trait points it adds.</summary>
/// <param name="Id">Option id (send it back on save to keep the option).</param>
/// <param name="Text">Option text.</param>
/// <param name="Points">Trait points.</param>
public sealed record QuestionOptionDto(int Id, string Text, IReadOnlyList<TraitPointsDto> Points);

/// <summary>Points an option adds to a trait.</summary>
public sealed record TraitPointsDto(int TraitId, string TraitName, int Points);

/// <summary>Create or replace a question with its options.</summary>
/// <param name="Text">Question text (5–500 characters).</param>
/// <param name="IsActive">False to take it out of the quiz.</param>
/// <param name="Options">2–6 options. Send an option's <c>id</c> to keep it; options left out are removed (only when never chosen).</param>
public sealed record SaveQuestionRequest(string Text, bool IsActive, SaveOptionRequest[] Options);

/// <summary>An answer option.</summary>
/// <param name="Id">Existing option id, or null for a new one.</param>
/// <param name="Text">Option text (1–300 characters).</param>
/// <param name="Points">Trait points (each trait once, 1–5 points).</param>
public sealed record SaveOptionRequest(int? Id, string Text, SaveTraitPointsRequest[] Points);

/// <summary>Points an option adds to a trait.</summary>
/// <param name="TraitId">Trait.</param>
/// <param name="Points">1–5.</param>
public sealed record SaveTraitPointsRequest(int TraitId, int Points);

/// <summary>A career path with its trait weights and roadmap.</summary>
/// <param name="Id">Career id.</param>
/// <param name="Name">Name.</param>
/// <param name="Description">Description, or null.</param>
/// <param name="IsActive">Inactive careers are hidden from students.</param>
/// <param name="Traits">Trait weights (1–10).</param>
/// <param name="Steps">Roadmap steps in order.</param>
public sealed record CareerPathDto(
    int Id, string Name, string? Description, bool IsActive, IReadOnlyList<TraitWeightDto> Traits, IReadOnlyList<RoadmapStepDto> Steps);

/// <summary>How much a trait matters for a career.</summary>
public sealed record TraitWeightDto(int TraitId, string TraitName, int Weight);

/// <summary>A roadmap step as the admin edits it.</summary>
/// <param name="Id">Step id.</param>
/// <param name="StepOrder">1-based position.</param>
/// <param name="Title">Title.</param>
/// <param name="Description">Description, or null.</param>
/// <param name="CourseId">Linked course, or null.</param>
/// <param name="CourseCode">Linked course code, or null.</param>
/// <param name="CourseName">Linked course name, or null.</param>
public sealed record RoadmapStepDto(int Id, int StepOrder, string Title, string? Description, int? CourseId, string? CourseCode, string? CourseName);

/// <summary>Create or replace a career path. Trait weights and roadmap steps are replaced by what is sent.</summary>
/// <param name="Name">Unique name (2–100 characters).</param>
/// <param name="Description">Optional, up to 1000 characters.</param>
/// <param name="IsActive">False to hide it from students.</param>
/// <param name="Traits">At least one trait weight; each trait once; weight 1–10.</param>
/// <param name="Steps">Roadmap steps in order (0–20).</param>
public sealed record SaveCareerPathRequest(string Name, string? Description, bool IsActive, SaveTraitWeightRequest[] Traits, SaveRoadmapStepRequest[] Steps);

/// <summary>How much a trait matters for the career.</summary>
public sealed record SaveTraitWeightRequest(int TraitId, int Weight);

/// <summary>A roadmap step.</summary>
/// <param name="Title">Title (2–150 characters).</param>
/// <param name="Description">Optional, up to 1000 characters.</param>
/// <param name="CourseId">Optional linked course.</param>
public sealed record SaveRoadmapStepRequest(string Title, string? Description, int? CourseId);

/// <summary>A course and the traits it is tagged with.</summary>
/// <param name="CourseId">Course id.</param>
/// <param name="Code">Course code.</param>
/// <param name="Name">Course name.</param>
/// <param name="TraitIds">Tagged traits.</param>
public sealed record CourseTagsDto(int CourseId, string Code, string Name, IReadOnlyList<int> TraitIds);

/// <summary>Replace a course's trait tags.</summary>
/// <param name="TraitIds">Traits (each once, at most 10). Empty removes every tag.</param>
public sealed record SaveCourseTagsRequest(int[] TraitIds);
