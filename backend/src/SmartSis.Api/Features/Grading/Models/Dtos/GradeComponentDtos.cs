using Microsoft.AspNetCore.Mvc;

namespace SmartSis.Api.Features.Grading.Models.Dtos;

/// <summary>Which course and term to list components for.</summary>
/// <param name="CourseId">Course id.</param>
/// <param name="TermId">Term id.</param>
public sealed record GradeComponentQuery(
    [property: FromQuery(Name = "courseId")] int CourseId = 0,
    [property: FromQuery(Name = "termId")] int TermId = 0);

/// <summary>The grade components of one course in one term.</summary>
/// <param name="CourseId">Course id.</param>
/// <param name="TermId">Term id.</param>
/// <param name="TotalWeight">Sum of the weights (at most 100).</param>
/// <param name="IsFinalized">True once an instructor finalized a section of this course in this term; components are then locked.</param>
/// <param name="Items">Components, in the order they were created.</param>
public sealed record GradeComponentListDto(int CourseId, int TermId, decimal TotalWeight, bool IsFinalized, IReadOnlyList<GradeComponentDto> Items);

/// <summary>A grade component (e.g. "Midterm", out of 50, weighing 30%).</summary>
/// <param name="Id">Component id.</param>
/// <param name="CourseId">Course id.</param>
/// <param name="TermId">Term id.</param>
/// <param name="Name">Name, unique per course and term.</param>
/// <param name="MaxGrade">Highest possible score.</param>
/// <param name="Weight">Share of the final grade, in percent.</param>
/// <param name="ScoreCount">Scores already entered. A component with scores can't be deleted.</param>
public sealed record GradeComponentDto(int Id, int CourseId, int TermId, string Name, decimal MaxGrade, decimal Weight, int ScoreCount);

/// <summary>Create a component. Weights of a course in a term add up to at most 100 (rule G1).</summary>
/// <param name="CourseId">Course id.</param>
/// <param name="TermId">Term id.</param>
/// <param name="Name">1–64 characters, unique per course and term.</param>
/// <param name="MaxGrade">Highest possible score, 0.01–1000.</param>
/// <param name="Weight">Percent of the final grade, 0.01–100.</param>
public sealed record CreateGradeComponentRequest(int CourseId, int TermId, string Name, decimal MaxGrade, decimal Weight);

/// <summary>Edit a component's name, max grade and weight. Its course and term never change.</summary>
/// <param name="Name">1–64 characters, unique per course and term.</param>
/// <param name="MaxGrade">Highest possible score, 0.01–1000; not below a score already entered.</param>
/// <param name="Weight">Percent of the final grade, 0.01–100.</param>
public sealed record UpdateGradeComponentRequest(string Name, decimal MaxGrade, decimal Weight);
