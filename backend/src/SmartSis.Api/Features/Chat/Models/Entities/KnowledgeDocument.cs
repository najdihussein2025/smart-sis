using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Chat.Models.Entities;

/// <summary>University policy / FAQ uploaded by an admin for the chatbot.</summary>
public class KnowledgeDocument : BaseEntity
{
    public string Title { get; set; } = "";
    public string FileUrl { get; set; } = "";

    /// <summary>Users.Id of the admin.</summary>
    public int UploadedBy { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.Processing;

    public ICollection<KnowledgeChunk> Chunks { get; set; } = [];
}
