using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Announcements.Interfaces;
using SmartSis.Api.Features.Announcements.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Announcements.Controllers;

/// <summary>Any signed-in user: the announcements feed (PB-12).</summary>
[ApiController]
[Authorize]
[Route("api/announcements")]
[Tags("Announcements")]
public sealed class AnnouncementsController(IAnnouncementService announcements) : ControllerBase
{
    /// <summary>
    /// My feed, newest first: published and not expired announcements meant for me, plus my own scheduled ones.
    /// Student: All, Students, my major and sections I'm enrolled in. Instructor: All, Instructors and sections I teach.
    /// Admin: every live announcement. <c>status</c> is ignored here.
    /// </summary>
    [HttpGet]
    public Task<PagedResult<AnnouncementDto>> Feed([FromQuery] AnnouncementListQuery query, CancellationToken ct) =>
        announcements.FeedAsync(query, ct);
}

/// <summary>Admin: post, edit and delete announcements for everyone, students, instructors or a major (rule C1).</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/announcements")]
[Tags("Admin: Announcements")]
public sealed class AdminAnnouncementsController(IAnnouncementService announcements) : ControllerBase
{
    /// <summary>Every announcement (any author, scheduled and expired too), newest publish date first.</summary>
    [HttpGet]
    public Task<PagedResult<AnnouncementDto>> List([FromQuery] AnnouncementListQuery query, CancellationToken ct) =>
        announcements.ListAllAsync(query, ct);

    /// <summary>Post an announcement. Live posts notify their audience at once; scheduled ones when PublishAt comes.</summary>
    /// <response code="201">Created.</response>
    /// <response code="400">Invalid fields, a Section scope, or a major that doesn't exist.</response>
    [HttpPost]
    [ProducesResponseType<AnnouncementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnouncementDto>> Create(SaveAnnouncementRequest request, CancellationToken ct)
    {
        var created = await announcements.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Edit any announcement. An instructor's section post keeps its section (send Scope "Section" and the same SectionId).</summary>
    /// <response code="400">Invalid fields.</response>
    /// <response code="404">No announcement with this id.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<AnnouncementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AnnouncementDto> Update(int id, SaveAnnouncementRequest request, CancellationToken ct) =>
        announcements.UpdateAsync(id, request, ct);

    /// <summary>Delete any announcement. Notifications already sent stay in the bell.</summary>
    /// <response code="404">No announcement with this id.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await announcements.DeleteAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Instructor: post, edit and delete announcements for sections they teach (rule C1).</summary>
[ApiController]
[Authorize(Policy = Policies.Instructor)]
[Route("api/instructor/announcements")]
[Tags("Instructor: Announcements")]
public sealed class InstructorAnnouncementsController(IAnnouncementService announcements) : ControllerBase
{
    /// <summary>Announcements I posted (scheduled and expired too), newest publish date first.</summary>
    [HttpGet]
    public Task<PagedResult<AnnouncementDto>> List([FromQuery] AnnouncementListQuery query, CancellationToken ct) =>
        announcements.ListMineAsync(query, ct);

    /// <summary>Sections I teach in the current term, to post to.</summary>
    [HttpGet("sections")]
    public Task<List<AnnouncementSectionDto>> Sections(CancellationToken ct) => announcements.MySectionsAsync(ct);

    /// <summary>Post to one of my sections. Live posts notify the enrolled students at once.</summary>
    /// <response code="201">Created.</response>
    /// <response code="400">Invalid fields or a section that doesn't exist.</response>
    /// <response code="403">I don't teach this section.</response>
    [HttpPost]
    [ProducesResponseType<AnnouncementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnnouncementDto>> Create(SaveSectionAnnouncementRequest request, CancellationToken ct)
    {
        var created = await announcements.CreateForSectionAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Edit one of my announcements.</summary>
    /// <response code="400">Invalid fields.</response>
    /// <response code="403">Not my announcement, or a section I don't teach.</response>
    /// <response code="404">No announcement with this id.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType<AnnouncementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<AnnouncementDto> Update(int id, SaveSectionAnnouncementRequest request, CancellationToken ct) =>
        announcements.UpdateForSectionAsync(id, request, ct);

    /// <summary>Delete one of my announcements.</summary>
    /// <response code="403">Not my announcement.</response>
    /// <response code="404">No announcement with this id.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await announcements.DeleteAsync(id, ct);
        return NoContent();
    }
}
