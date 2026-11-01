using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Users.Interfaces;

/// <summary>Admin management of student and instructor accounts (PB-03, SR-20, UC-15).</summary>
public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> SearchAsync(UserSearchQuery query, CancellationToken ct);
    Task<UserDetailDto> GetAsync(int userId, CancellationToken ct);
    Task<UserDetailDto> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<UserDetailDto> UpdateAsync(int userId, UpdateUserRequest request, CancellationToken ct);

    /// <summary>Soft delete (IsActive = false, DeletedAt set). Idempotent.</summary>
    Task DeactivateAsync(int userId, CancellationToken ct);
}
