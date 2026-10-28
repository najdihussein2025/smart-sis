using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Chat.Data;

internal sealed class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> b)
    {
        b.ToTable("ChatConversations");
        b.Property(x => x.Title).HasMaxLength(200);
        b.HasIndex(x => x.StudentId);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
    }
}

internal sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> b)
    {
        b.ToTable("ChatMessages");
        b.Property(x => x.Content).HasColumnType("nvarchar(max)");
        b.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        b.HasOne(x => x.Conversation).WithMany(c => c.Messages).HasForeignKey(x => x.ConversationId);
    }
}

internal sealed class KnowledgeDocumentConfiguration : IEntityTypeConfiguration<KnowledgeDocument>
{
    public void Configure(EntityTypeBuilder<KnowledgeDocument> b)
    {
        b.ToTable("KnowledgeDocuments");
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.FileUrl).HasMaxLength(1024);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedBy);
    }
}

internal sealed class KnowledgeChunkConfiguration : IEntityTypeConfiguration<KnowledgeChunk>
{
    public void Configure(EntityTypeBuilder<KnowledgeChunk> b)
    {
        b.ToTable("KnowledgeChunks");
        b.Property(x => x.Content).HasColumnType("nvarchar(max)");
        b.Property(x => x.Embedding).HasColumnType("varbinary(max)");
        b.HasOne(x => x.Document).WithMany(d => d.Chunks).HasForeignKey(x => x.DocumentId);
    }
}
