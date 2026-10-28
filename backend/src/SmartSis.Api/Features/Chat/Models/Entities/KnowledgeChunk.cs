using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Chat.Models.Entities;

/// <summary>Written only by the Python AI service during ingest.</summary>
public class KnowledgeChunk : BaseEntity
{
    public int DocumentId { get; set; }
    public KnowledgeDocument Document { get; set; } = null!;
    public string Content { get; set; } = "";

    /// <summary>float32 little-endian vector. varbinary(max) until SQL Server 2025 VECTOR is confirmed (PROJECT.md §11).</summary>
    public byte[] Embedding { get; set; } = [];
}
