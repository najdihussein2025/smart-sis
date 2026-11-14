using System.Text;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Features.Chat.Models.Dtos;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Chat.Services;

/// <summary>
/// Admin knowledge base (T61/T62). The uploaded text is kept by <see cref="IKnowledgeFileStore"/> (its key goes in
/// <c>FileUrl</c>) and sent inline to the AI service's <c>/ingest</c>, which writes the KnowledgeChunks. Ingest runs
/// during the request (documents are small); if the AI service fails, the document is marked Failed and can be re-ingested.
/// </summary>
public sealed class KnowledgeService(
    IKnowledgeRepository docs, IKnowledgeFileStore files, IAiServiceClient ai, ICurrentUser me, IUnitOfWork uow,
    ILogger<KnowledgeService> logger) : IKnowledgeService
{
    public async Task<IReadOnlyList<KnowledgeDocumentDto>> ListAsync(CancellationToken ct) => await docs.ListAsync(ct);

    public async Task<KnowledgeDocumentDto> UploadAsync(UploadKnowledgeRequest request, CancellationToken ct)
    {
        var file = request.File ?? throw new BusinessRuleException("Choose a file.", "no_file"); // the validator already checks this
        string content;
        using (var reader = new StreamReader(file.OpenReadStream(), new UTF8Encoding(false, throwOnInvalidBytes: true)))
        {
            try
            {
                content = await reader.ReadToEndAsync(ct);
            }
            catch (DecoderFallbackException)
            {
                throw new BusinessRuleException("The file must be UTF-8 text (.txt or .md).", "invalid_text");
            }
        }
        if (string.IsNullOrWhiteSpace(content))
            throw new BusinessRuleException("The file is empty.", "empty_file");

        var key = await files.SaveAsync(content, Path.GetExtension(file.FileName).ToLowerInvariant(), ct);
        var doc = new KnowledgeDocument { Title = request.Title.Trim(), FileUrl = key, UploadedBy = me.UserId, Status = KnowledgeStatus.Processing };
        docs.Add(doc);
        await uow.SaveChangesAsync(ct);

        await IngestAsync(doc, content, ct);
        return (await docs.GetDtoAsync(doc.Id, ct))!;
    }

    public async Task<KnowledgeDocumentDto> ReingestAsync(int id, CancellationToken ct)
    {
        var doc = await docs.FindAsync(id, ct) ?? throw NotFoundException.For<KnowledgeDocument>(id);
        var content = doc.FileUrl == "" ? null : await files.ReadAsync(doc.FileUrl, ct);
        if (content is null)
            throw new BusinessRuleException("The original file of this document isn't stored. Upload it again.", "file_missing");

        doc.Status = KnowledgeStatus.Processing;
        await uow.SaveChangesAsync(ct);
        await IngestAsync(doc, content, ct);
        return (await docs.GetDtoAsync(doc.Id, ct))!;
    }

    /// <summary>Removes the document, its chunks and its file. Knowledge documents aren't user records, so this is a real delete.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var doc = await docs.FindAsync(id, ct) ?? throw NotFoundException.For<KnowledgeDocument>(id);
        await uow.ExecuteInTransactionAsync(async () =>
        {
            docs.RemoveChunks(await docs.ChunksAsync(doc.Id, ct));
            docs.Remove(doc);
            await uow.SaveChangesAsync(ct);
        }, ct);
        if (doc.FileUrl != "") files.Delete(doc.FileUrl);
    }

    private async Task IngestAsync(KnowledgeDocument doc, string content, CancellationToken ct)
    {
        try
        {
            await ai.IngestAsync(doc.Id, doc.Title, content, ct);
            doc.Status = KnowledgeStatus.Ready;
        }
        catch (BusinessRuleException ex)
        {
            logger.LogWarning("Ingest of knowledge document {DocumentId} failed: {Reason}", doc.Id, ex.Message);
            doc.Status = KnowledgeStatus.Failed;
        }
        await uow.SaveChangesAsync(CancellationToken.None);
    }
}
