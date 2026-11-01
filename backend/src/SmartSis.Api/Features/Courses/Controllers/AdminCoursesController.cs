using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Courses.Interfaces;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Courses.Controllers;

/// <summary>Admin: create and edit courses, their prerequisites and major / elective mapping.</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/courses")]
[Tags("Admin: Courses")]
public sealed class AdminCoursesController(ICourseAdminService courses) : ControllerBase
{
    /// <summary>Search courses, ordered by code. <c>search</c> matches code or name.</summary>
    [HttpGet]
    public Task<PagedResult<CourseListItemDto>> List([FromQuery] CourseSearchQuery query, CancellationToken ct) =>
        courses.SearchAsync(query, ct);

    /// <summary>Get one course with prerequisites and majors.</summary>
    /// <response code="404">No course with this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<CourseDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CourseDetailDto> Get(int id, CancellationToken ct) => courses.GetAsync(id, ct);

    /// <summary>Create a course. The code is stored upper case with one space ("csc101" → "CSC 101").</summary>
    /// <response code="201">Created; <c>Location</c> points to the new course.</response>
    /// <response code="400">Invalid fields, or a department / major / prerequisite that doesn't exist (field errors in <c>errors</c>).</response>
    /// <response code="409">Code already used (<c>code: duplicate_course_code</c>).</response>
    [HttpPost]
    [ProducesResponseType<CourseDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseDetailDto>> Create(SaveCourseRequest request, CancellationToken ct)
    {
        var course = await courses.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = course.Id }, course);
    }

    /// <summary>Replace a course's fields, prerequisite list and major list with what is sent.</summary>
    /// <response code="400">Invalid fields, or a department / major / prerequisite that doesn't exist.</response>
    /// <response code="404">No course with this id.</response>
    /// <response code="409">Code already used (<c>duplicate_course_code</c>), course listed as its own prerequisite (<c>prerequisite_self</c>) or a prerequisite cycle (<c>prerequisite_cycle</c>).</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<CourseDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<CourseDetailDto> Update(int id, SaveCourseRequest request, CancellationToken ct) =>
        courses.UpdateAsync(id, request, ct);
}
