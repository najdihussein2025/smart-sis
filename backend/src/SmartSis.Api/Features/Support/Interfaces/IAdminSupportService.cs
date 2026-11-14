using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Interfaces;

/// <summary>Admin support inbox (PB-14). Every request is handled by admins (rule C3).</summary>
public interface IAdminSupportService
{
    Task<PagedResult<AdminSupportRequestDto>> SearchAsync(AdminSupportSearchQuery query, CancellationToken ct);
    Task<AdminSupportRequestDetailDto> GetAsync(int id, CancellationToken ct);
    Task<AdminSupportRequestDetailDto> ChangeStatusAsync(int id, ChangeSupportStatusRequest request, CancellationToken ct);
    Task<AdminSupportRequestDetailDto> AssignAsync(int id, AssignSupportRequestRequest request, CancellationToken ct);
    Task<AdminSupportRequestDetailDto> ReplyAsync(int id, SupportReplyRequest request, CancellationToken ct);
    Task<List<SupportAdminDto>> ListAdminsAsync(CancellationToken ct);
}

/// <summary>Admin management of support categories.</summary>
public interface ISupportCategoryService
{
    Task<List<AdminSupportCategoryDto>> ListAsync(CancellationToken ct);
    Task<AdminSupportCategoryDto> CreateAsync(SaveSupportCategoryRequest request, CancellationToken ct);
    Task<AdminSupportCategoryDto> RenameAsync(int id, SaveSupportCategoryRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
