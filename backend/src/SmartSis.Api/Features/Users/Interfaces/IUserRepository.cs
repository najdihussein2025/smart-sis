using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Users.Interfaces;

/// <summary>
/// User queries. The global soft-delete filter hides deactivated users everywhere, except the methods
/// marked "includes deactivated", which call IgnoreQueryFilters() on purpose (admin views and duplicate checks).
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByUniversityIdAsync(long universityId, CancellationToken ct);

    /// <summary>User with Student (Major, Campus) or Instructor (Department) loaded. No tracking.</summary>
    Task<User?> GetWithProfileAsync(int userId, CancellationToken ct);

    /// <summary>Admin list, ordered by full name. Includes deactivated (filtered by <see cref="UserSearchQuery.Status"/>).</summary>
    Task<PagedResult<UserListItemDto>> SearchAsync(UserSearchQuery query, PageQuery page, CancellationToken ct);

    /// <summary>Admin detail projection. Includes deactivated.</summary>
    Task<UserDetailDto?> GetDetailAsync(int userId, CancellationToken ct);

    /// <summary>Tracked user with Student / Instructor rows, for editing or deactivating. Includes deactivated.</summary>
    Task<User?> GetForAdminAsync(int userId, CancellationToken ct);

    /// <summary>Includes deactivated (the unique index covers every row).</summary>
    Task<bool> UniversityIdExistsAsync(long universityId, CancellationToken ct);

    /// <summary>Case-insensitive; email already normalized. Includes deactivated. Optionally ignores one user.</summary>
    Task<bool> EmailExistsAsync(string email, int? excludeUserId, CancellationToken ct);

    Task<bool> MajorExistsAsync(int majorId, CancellationToken ct);
    Task<bool> CampusExistsAsync(int campusId, CancellationToken ct);
    Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken ct);
}
