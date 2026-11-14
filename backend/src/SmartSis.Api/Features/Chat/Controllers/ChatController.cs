using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Chat.Controllers;

/// <summary>Student: the AI assistant and tutor (PB-15, PB-19). Only the student's own conversations are visible.</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/chat")]
[Tags("Student: Assistant")]
public sealed class ChatController(IChatService chat) : ControllerBase
{
    /// <summary>
    /// Send a message. Without <c>conversationId</c> a new conversation starts (Assistant, or Tutor for an enrolled course).
    /// Your own sign-in token is passed to the AI service, so it can only read your own data.
    /// </summary>
    /// <response code="400">Empty or too long message; Tutor without a course.</response>
    /// <response code="404">The conversation doesn't exist (or isn't yours).</response>
    /// <response code="409"><c>not_enrolled</c> (tutor course) or <c>assistant_unavailable</c>.</response>
    [HttpPost("messages")]
    [ProducesResponseType<ChatReplyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ChatReplyDto> Send(SendChatMessageRequest request, CancellationToken ct) => chat.SendAsync(request, ct);

    /// <summary>Your conversations, most recent first.</summary>
    [HttpGet("conversations")]
    public Task<IReadOnlyList<ConversationSummaryDto>> List(CancellationToken ct) => chat.ListAsync(ct);

    /// <summary>One of your conversations with its messages.</summary>
    /// <response code="404">The conversation doesn't exist (or isn't yours).</response>
    [HttpGet("conversations/{id:int}")]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ConversationDto> Get(int id, CancellationToken ct) => chat.GetAsync(id, ct);

    /// <summary>Rename one of your conversations.</summary>
    /// <response code="404">The conversation doesn't exist (or isn't yours).</response>
    [HttpPut("conversations/{id:int}")]
    [ProducesResponseType<ConversationSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ConversationSummaryDto> Rename(int id, RenameConversationRequest request, CancellationToken ct) =>
        chat.RenameAsync(id, request, ct);

    /// <summary>Delete one of your conversations (kept in the database as deleted).</summary>
    /// <response code="404">The conversation doesn't exist (or isn't yours).</response>
    [HttpDelete("conversations/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await chat.DeleteAsync(id, ct);
        return NoContent();
    }
}
