using SmartSis.Api.Features.Chat.Enums;

namespace SmartSis.Api.Features.Chat.Models.Dtos;

/// <summary>Upload a policy / FAQ document (multipart form). Plain text or Markdown, up to 1 MB.</summary>
public sealed class UploadKnowledgeRequest
{
    /// <summary>Title shown to admins and cited by the assistant.</summary>
    public string Title { get; set; } = "";

    /// <summary>A .txt or .md file, UTF-8.</summary>
    public IFormFile? File { get; set; }
}

/// <summary>A knowledge-base document.</summary>
/// <param name="Id">Document id.</param>
/// <param name="Title">Title.</param>
/// <param name="Status">"Processing", "Ready" (searchable) or "Failed" (re-ingest it).</param>
/// <param name="Chunks">Number of searchable passages.</param>
/// <param name="CanReingest">False when the original file isn't stored (e.g. seeded demo documents).</param>
/// <param name="UploadedByName">The admin who uploaded it.</param>
/// <param name="CreatedAt">Uploaded (UTC).</param>
/// <param name="UpdatedAt">Last status change (UTC).</param>
public sealed record KnowledgeDocumentDto(
    int Id, string Title, KnowledgeStatus Status, int Chunks, bool CanReingest, string? UploadedByName, DateTime CreatedAt, DateTime UpdatedAt);
