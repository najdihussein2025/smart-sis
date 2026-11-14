using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Courses.Interfaces;
using SmartSis.Api.Features.Courses.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Courses.Controllers;

/// <summary>Student: browse the current term's courses and sections (PB-04, T13).</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/catalog")]
[Tags("Student: Catalog")]
public sealed class CatalogController(ICatalogService catalog) : ControllerBase
{
    /// <summary>Courses offered this term for a major (the student's own by default), with the Major / Elective type.</summary>
    [HttpGet("courses")]
    public Task<CatalogDto> List([FromQuery] CatalogQuery query, CancellationToken ct) => catalog.ListAsync(query, ct);

    /// <summary>Course details: description, prerequisites (and whether passed), and this term's sections with seats left.</summary>
    /// <response code="404">No course with this id.</response>
    [HttpGet("courses/{id:int}")]
    [ProducesResponseType<CatalogCourseDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CatalogCourseDetailDto> Get(int id, CancellationToken ct) => catalog.GetCourseAsync(id, ct);

    /// <summary>Majors, for the catalog's major filter.</summary>
    [HttpGet("majors")]
    public Task<IReadOnlyList<CatalogMajorDto>> Majors(CancellationToken ct) => catalog.ListMajorsAsync(ct);
}
