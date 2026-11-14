using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Chat.Repositories;

public sealed class KnowledgeRepository(SisDbContext db) : IKnowledgeRepository
{
    private IQueryable<KnowledgeDocumentDto> Dtos()
    {
        var users = db.Set<User>().IgnoreQueryFilters();
        return db.Set<KnowledgeDocument>().AsNoTracking()
            .Select(d => new KnowledgeDocumentDto(
                d.Id, d.Title, d.Status, d.Chunks.Count, d.FileUrl != "",
                users.Where(u => u.Id == d.UploadedBy).Select(u => u.FullName).FirstOrDefault(),
                d.CreatedAt, d.UpdatedAt));
    }

    public Task<List<KnowledgeDocumentDto>> ListAsync(CancellationToken ct) =>
        Dtos().OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id).ToListAsync(ct);

    public Task<KnowledgeDocumentDto?> GetDtoAsync(int id, CancellationToken ct) =>
        Dtos().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<KnowledgeDocument?> FindAsync(int id, CancellationToken ct) =>
        db.Set<KnowledgeDocument>().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<List<KnowledgeChunk>> ChunksAsync(int documentId, CancellationToken ct) =>
        db.Set<KnowledgeChunk>().Where(c => c.DocumentId == documentId).ToListAsync(ct);

    public void Add(KnowledgeDocument document) => db.Set<KnowledgeDocument>().Add(document);
    public void Remove(KnowledgeDocument document) => db.Set<KnowledgeDocument>().Remove(document);
    public void RemoveChunks(IEnumerable<KnowledgeChunk> chunks) => db.Set<KnowledgeChunk>().RemoveRange(chunks);
}
