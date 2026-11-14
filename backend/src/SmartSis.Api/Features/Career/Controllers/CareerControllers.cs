using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Career.Interfaces;
using SmartSis.Api.Features.Career.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Career.Controllers;

/// <summary>Student: career assessment (PB-16), recommendations (PB-17) and roadmaps (PB-20).</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/career")]
[Tags("Student: Career")]
public sealed class CareerController(ICareerService career) : ControllerBase
{
    /// <summary>The student's own assessments, newest first, and whether they can take one now (one per term).</summary>
    [HttpGet("assessments")]
    public Task<MyAssessmentsDto> MyAssessments(CancellationToken ct) => career.GetMyAssessmentsAsync(ct);

    /// <summary>The quiz: 20–30 active questions with their options (trait points are not sent).</summary>
    /// <response code="409"><c>no_current_term</c>, <c>already_taken</c> (this term) or <c>not_enough_questions</c>.</response>
    [HttpGet("quiz")]
    [ProducesResponseType<QuizDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<QuizDto> Quiz(CancellationToken ct) => career.GetQuizAsync(ct);

    /// <summary>Submit the quiz: every quiz question answered exactly once.</summary>
    /// <response code="201">Saved, with the trait scores.</response>
    /// <response code="400">A question answered twice, or an option of another question (field errors in <c>errors</c>).</response>
    /// <response code="409"><c>no_current_term</c>, <c>already_taken</c>, <c>not_enough_questions</c>, <c>quiz_changed</c> or <c>quiz_incomplete</c>.</response>
    [HttpPost("assessments")]
    [ProducesResponseType<AssessmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssessmentDto>> Submit(SubmitAssessmentRequest request, CancellationToken ct)
    {
        var result = await career.SubmitAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Top 3 careers for the latest assessment, an explanation and suggested courses.</summary>
    /// <response code="409"><c>assessment_required</c>: no assessment yet.</response>
    [HttpGet("recommendations")]
    [ProducesResponseType<RecommendationsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<RecommendationsDto> Recommendations(CancellationToken ct) => career.GetRecommendationsAsync(ct);

    /// <summary>Active careers, for choosing a roadmap.</summary>
    [HttpGet("careers")]
    public Task<IReadOnlyList<CareerSummaryDto>> Careers(CancellationToken ct) => career.ListCareersAsync(ct);

    /// <summary>A career's roadmap with the student's progress: Done, InProgress, Next, Later or Milestone.</summary>
    /// <response code="404">No active career with this id.</response>
    [HttpGet("careers/{id:int}/roadmap")]
    [ProducesResponseType<RoadmapDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<RoadmapDto> Roadmap(int id, CancellationToken ct) => career.GetRoadmapAsync(id, ct);
}

/// <summary>Admin: traits, the question bank, careers with roadmaps and course-trait tags.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/career")]
[Tags("Admin: Career")]
public sealed class AdminCareerController(ICareerAdminService admin) : ControllerBase
{
    /// <summary>All traits by name.</summary>
    [HttpGet("traits")]
    public Task<IReadOnlyList<TraitDto>> Traits(CancellationToken ct) => admin.ListTraitsAsync(ct);

    /// <summary>Create a trait.</summary>
    /// <response code="409"><c>duplicate_trait</c>.</response>
    [HttpPost("traits")]
    [ProducesResponseType<TraitDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TraitDto>> CreateTrait(SaveTraitRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await admin.CreateTraitAsync(request, ct));

    /// <summary>Rename or describe a trait.</summary>
    /// <response code="404">No trait with this id.</response>
    /// <response code="409"><c>duplicate_trait</c>.</response>
    [HttpPut("traits/{id:int}")]
    [ProducesResponseType<TraitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<TraitDto> UpdateTrait(int id, SaveTraitRequest request, CancellationToken ct) => admin.UpdateTraitAsync(id, request, ct);

    /// <summary>The whole question bank, active first, with the active count the quiz needs.</summary>
    [HttpGet("questions")]
    public Task<QuestionBankDto> Questions(CancellationToken ct) => admin.ListQuestionsAsync(ct);

    /// <summary>One question with its options and trait points.</summary>
    /// <response code="404">No question with this id.</response>
    [HttpGet("questions/{id:int}")]
    [ProducesResponseType<QuestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<QuestionDto> Question(int id, CancellationToken ct) => admin.GetQuestionAsync(id, ct);

    /// <summary>Create a question with 2–6 options; each option adds 1–5 points to traits.</summary>
    /// <response code="400">Invalid fields or a trait that doesn't exist.</response>
    [HttpPost("questions")]
    [ProducesResponseType<QuestionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(SaveQuestionRequest request, CancellationToken ct)
    {
        var q = await admin.CreateQuestionAsync(request, ct);
        return CreatedAtAction(nameof(Question), new { id = q.Id }, q);
    }

    /// <summary>Replace a question and its options. Send an option's <c>id</c> to keep it. Use <c>isActive: false</c> to deactivate.</summary>
    /// <response code="400">Invalid fields, a trait that doesn't exist or an option of another question.</response>
    /// <response code="404">No question with this id.</response>
    /// <response code="409"><c>option_answered</c>: removing an option students already chose.</response>
    [HttpPut("questions/{id:int}")]
    [ProducesResponseType<QuestionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<QuestionDto> UpdateQuestion(int id, SaveQuestionRequest request, CancellationToken ct) =>
        admin.UpdateQuestionAsync(id, request, ct);

    /// <summary>Delete a question that was never answered. Answered questions must be deactivated instead.</summary>
    /// <response code="404">No question with this id.</response>
    /// <response code="409"><c>question_answered</c>.</response>
    [HttpDelete("questions/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteQuestion(int id, CancellationToken ct)
    {
        await admin.DeleteQuestionAsync(id, ct);
        return NoContent();
    }

    /// <summary>All careers (active and inactive) with trait weights and roadmap steps.</summary>
    [HttpGet("careers")]
    public Task<IReadOnlyList<CareerPathDto>> Careers(CancellationToken ct) => admin.ListCareersAsync(ct);

    /// <summary>One career.</summary>
    /// <response code="404">No career with this id.</response>
    [HttpGet("careers/{id:int}")]
    [ProducesResponseType<CareerPathDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CareerPathDto> Career(int id, CancellationToken ct) => admin.GetCareerAsync(id, ct);

    /// <summary>Create a career with trait weights and roadmap steps.</summary>
    /// <response code="400">Invalid fields, or a trait / course that doesn't exist.</response>
    /// <response code="409"><c>duplicate_career</c>.</response>
    [HttpPost("careers")]
    [ProducesResponseType<CareerPathDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CareerPathDto>> CreateCareer(SaveCareerPathRequest request, CancellationToken ct)
    {
        var c = await admin.CreateCareerAsync(request, ct);
        return CreatedAtAction(nameof(Career), new { id = c.Id }, c);
    }

    /// <summary>Replace a career, its trait weights and its roadmap steps.</summary>
    /// <response code="400">Invalid fields, or a trait / course that doesn't exist.</response>
    /// <response code="404">No career with this id.</response>
    /// <response code="409"><c>duplicate_career</c>.</response>
    [HttpPut("careers/{id:int}")]
    [ProducesResponseType<CareerPathDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<CareerPathDto> UpdateCareer(int id, SaveCareerPathRequest request, CancellationToken ct) =>
        admin.UpdateCareerAsync(id, request, ct);

    /// <summary>Every course with the traits it is tagged with.</summary>
    [HttpGet("course-tags")]
    public Task<IReadOnlyList<CourseTagsDto>> CourseTags(CancellationToken ct) => admin.ListCourseTagsAsync(ct);

    /// <summary>Replace a course's trait tags.</summary>
    /// <response code="400">A trait that doesn't exist.</response>
    /// <response code="404">No course with this id.</response>
    [HttpPut("course-tags/{courseId:int}")]
    [ProducesResponseType<CourseTagsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CourseTagsDto> SaveCourseTags(int courseId, SaveCourseTagsRequest request, CancellationToken ct) =>
        admin.SaveCourseTagsAsync(courseId, request, ct);
}
