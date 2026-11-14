using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Services;

/// <summary>
/// The student only ever sees their own requests: another student's request answers 404, not 403, so ids can't be probed.
/// A reply on a Resolved request reopens it; a Closed request takes no replies (409).
/// </summary>
public sealed class SupportService(ISupportRepository repo, ICurrentUser me, IUnitOfWork uow, SupportNotifier notifier, TimeProvider clock)
    : ISupportService
{
    public Task<List<SupportCategoryDto>> ListCategoriesAsync(CancellationToken ct) => repo.ListCategoriesAsync(ct);

    public Task<PagedResult<SupportRequestSummaryDto>> ListMineAsync(MySupportRequestsQuery query, CancellationToken ct) =>
        repo.ListForStudentAsync(me.UserId, query.Status, new PageQuery(query.Page, query.PageSize), ct);

    public async Task<SupportRequestDetailDto> GetMineAsync(int id, CancellationToken ct) =>
        await repo.GetStudentDetailAsync(id, me.UserId, ct) ?? throw NotFound(id);

    public async Task<SupportRequestDetailDto> CreateAsync(CreateSupportRequestRequest request, CancellationToken ct)
    {
        if (await repo.FindCategoryAsync(request.CategoryId, ct) is null)
            throw new ValidationException([new ValidationFailure("categoryId", "Choose a category from the list.")]);

        var entity = new SupportRequest
        {
            StudentId = me.UserId,
            CategoryId = request.CategoryId,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
        };
        repo.Add(entity);
        await uow.SaveChangesAsync(ct);
        return await GetMineAsync(entity.Id, ct);
    }

    public async Task<SupportRequestDetailDto> ReplyAsync(int id, SupportReplyRequest request, CancellationToken ct)
    {
        var entity = await repo.GetForUpdateAsync(id, me.UserId, ct) ?? throw NotFound(id);
        entity.AddReply(me.UserId, request.Body.Trim());
        if (entity.Status == SupportStatus.Resolved)
            entity.ChangeStatus(SupportStatus.Open, clock.GetUtcNow().UtcDateTime);
        notifier.StudentReplied(entity);
        await uow.SaveChangesAsync(ct);
        return await GetMineAsync(id, ct);
    }

    private static NotFoundException NotFound(int id) => new($"Support request {id} was not found.");
}
