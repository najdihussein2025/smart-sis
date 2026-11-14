using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Chat.Controllers;

/// <summary>Admin: university policies and FAQs the assistant searches (knowledge base).</summary>
[ApiController]
[Authorize(Policy = Policies.Admin)]
[Route("api/admin/knowledge")]
[Tags("Admin: Knowledge base")]
public sealed class AdminKnowledgeController(IKnowledgeService knowledge) : ControllerBase
{
    /// <summary>All documents with their status (Processing / Ready / Failed) and passage count, newest first.</summary>
    [HttpGet]
    public Task<IReadOnlyList<KnowledgeDocumentDto>> List(CancellationToken ct) => knowledge.ListAsync(ct);

    /// <summary>Upload a .txt or .md document (multipart form: <c>title</c>, <c>file</c>). It is split, embedded and becomes searchable.</summary>
    /// <response code="201">Saved. <c>status</c> is Ready, or Failed when the AI service couldn't process it (re-ingest later).</response>
    /// <response code="400">Missing title or file, wrong file type, or too large.</response>
    /// <response code="409">The file isn't UTF-8 text or is empty.</response>
    [HttpPost]
    [RequestSizeLimit(2_000_000)]
    [ProducesResponseType<KnowledgeDocumentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KnowledgeDocumentDto>> Upload([FromForm] UploadKnowledgeRequest request, CancellationToken ct)
    {
        var doc = await knowledge.UploadAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, doc);
    }

    /// <summary>Process the stored file again (after a failure, or after the embedding method changed).</summary>
    /// <response code="404">No document with this id.</response>
    /// <response code="409"><c>file_missing</c>: the original file isn't stored.</response>
    [HttpPost("{id:int}/reingest")]
    [ProducesResponseType<KnowledgeDocumentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<KnowledgeDocumentDto> Reingest(int id, CancellationToken ct) => knowledge.ReingestAsync(id, ct);

    /// <summary>Delete a document and its passages. The assistant stops using it immediately.</summary>
    /// <response code="404">No document with this id.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await knowledge.DeleteAsync(id, ct);
        return NoContent();
    }
}
