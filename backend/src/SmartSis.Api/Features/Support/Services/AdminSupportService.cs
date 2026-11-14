using FluentValidation;
using FluentValidation.Results;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Services;

/// <summary>
/// Any admin can handle any request (rule C3). The student is told (notification + queued email) when an admin replies
/// and when the status changes. An admin reply on an Open request moves it to In progress.
/// </summary>
public sealed class AdminSupportService(ISupportRepository repo, ICurrentUser me, IUnitOfWork uow, SupportNotifier notifier, TimeProvider clock)
    : IAdminSupportService
{
    public Task<PagedResult<AdminSupportRequestDto>> SearchAsync(AdminSupportSearchQuery query, CancellationToken ct) =>
        repo.SearchAsync(
            query.Status,
            query.CategoryId,
            query.Assigned == SupportAssignment.Me ? me.UserId : null,
            query.Assigned == SupportAssignment.Unassigned,
            query.Search,
            new PageQuery(query.Page, query.PageSize),
            ct);

    public async Task<AdminSupportRequestDetailDto> GetAsync(int id, CancellationToken ct) =>
        await repo.GetAdminDetailAsync(id, ct) ?? throw NotFound(id);

    public async Task<AdminSupportRequestDetailDto> ChangeStatusAsync(int id, ChangeSupportStatusRequest request, CancellationToken ct)
    {
        var entity = await repo.GetForUpdateAsync(id, null, ct) ?? throw NotFound(id);
        if (entity.ChangeStatus(request.Status, clock.GetUtcNow().UtcDateTime))
        {
            await notifier.StatusChangedAsync(entity, ct);
            await uow.SaveChangesAsync(ct);
        }
        return await GetAsync(id, ct);
    }

    public async Task<AdminSupportRequestDetailDto> AssignAsync(int id, AssignSupportRequestRequest request, CancellationToken ct)
    {
        var entity = await repo.GetForUpdateAsync(id, null, ct) ?? throw NotFound(id);
        if (request.AdminId is { } adminId && !await repo.IsActiveAdminAsync(adminId, ct))
            throw new ValidationException([new ValidationFailure("adminId", "Choose an active admin.")]);

        if (entity.AssignedAdminId != request.AdminId)
        {
            entity.AssignedAdminId = request.AdminId;
            if (request.AdminId is { } assignee && assignee != me.UserId) notifier.Assigned(entity, assignee);
            await uow.SaveChangesAsync(ct);
        }
        return await GetAsync(id, ct);
    }

    public async Task<AdminSupportRequestDetailDto> ReplyAsync(int id, SupportReplyRequest request, CancellationToken ct)
    {
        var entity = await repo.GetForUpdateAsync(id, null, ct) ?? throw NotFound(id);
        var body = request.Body.Trim();
        entity.AddReply(me.UserId, body);
        if (entity.Status == SupportStatus.Open)
            entity.ChangeStatus(SupportStatus.InProgress, clock.GetUtcNow().UtcDateTime);
        await notifier.AdminRepliedAsync(entity, body, ct);
        await uow.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public Task<List<SupportAdminDto>> ListAdminsAsync(CancellationToken ct) => repo.ListActiveAdminsAsync(ct);

    private static NotFoundException NotFound(int id) => new($"Support request {id} was not found.");
}
