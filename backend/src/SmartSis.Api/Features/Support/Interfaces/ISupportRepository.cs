using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Support.Interfaces;

/// <summary>Support requests, their threads and categories. Projections ignore the soft-delete filter so a deactivated
/// student's (or admin's) requests and replies stay visible.</summary>
public interface ISupportRepository : IRepository<SupportRequest>
{
    /// <summary>The student's requests, newest activity first.</summary>
    Task<PagedResult<SupportRequestSummaryDto>> ListForStudentAsync(int studentId, SupportStatus? status, PageQuery page, CancellationToken ct);

    /// <summary>Admin inbox, newest activity first.</summary>
    Task<PagedResult<AdminSupportRequestDto>> SearchAsync(
        SupportStatus? status, int? categoryId, int? assignedTo, bool unassignedOnly, string? search, PageQuery page, CancellationToken ct);

    /// <summary>The request with its thread, only when it belongs to <paramref name="studentId"/>.</summary>
    Task<SupportRequestDetailDto?> GetStudentDetailAsync(int id, int studentId, CancellationToken ct);

    Task<AdminSupportRequestDetailDto?> GetAdminDetailAsync(int id, CancellationToken ct);

    /// <summary>Tracked request, for changes. Pass <paramref name="studentId"/> to only find the student's own.</summary>
    Task<SupportRequest?> GetForUpdateAsync(int id, int? studentId, CancellationToken ct);

    /// <summary>Email and name of a user (even a deactivated one), or null.</summary>
    Task<(string Email, string FullName)?> GetContactAsync(int userId, CancellationToken ct);

    Task<bool> IsActiveAdminAsync(int userId, CancellationToken ct);

    Task<List<SupportAdminDto>> ListActiveAdminsAsync(CancellationToken ct);

    Task<List<SupportCategoryDto>> ListCategoriesAsync(CancellationToken ct);

    Task<List<AdminSupportCategoryDto>> ListCategoriesWithCountsAsync(CancellationToken ct);

    Task<SupportCategory?> FindCategoryAsync(int id, CancellationToken ct);

    /// <summary>Is the name taken (case-insensitive)? Optionally ignores one category.</summary>
    Task<bool> CategoryNameExistsAsync(string name, int? excludeId, CancellationToken ct);

    Task<int> CountRequestsInCategoryAsync(int categoryId, CancellationToken ct);

    void AddCategory(SupportCategory category);

    void RemoveCategory(SupportCategory category);
}
