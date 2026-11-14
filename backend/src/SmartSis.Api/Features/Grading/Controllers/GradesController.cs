using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Grading.Controllers;

/// <summary>Student: own grades and transcript (PB-09). The student comes from the token; there is no student id input.</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/grades")]
[Tags("Student: Grades")]
public sealed class GradesController(IStudentGradesService grades) : ControllerBase
{
    /// <summary>
    /// This term's courses: components with weight, score / max and %, "not graded" as a null score, the running % and the final
    /// letter once finalized; plus the term and cumulative GPA. Also used by the AI assistant.
    /// </summary>
    [HttpGet("my")]
    public Task<MyGradesDto> Mine(CancellationToken ct) => grades.GetMineAsync(ct);

    /// <summary>Every term with its courses, letters, term GPA, cumulative GPA and credits.</summary>
    [HttpGet("transcript")]
    public Task<TranscriptDto> Transcript(CancellationToken ct) => grades.GetTranscriptAsync(ct);
}
