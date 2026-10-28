using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Users.Interfaces;

/// <summary>
/// The Users feature's API for other features (Auth, Notifications…). Only this feature changes User rows.
/// Methods that change state do not save; the caller's IUnitOfWork commits.
/// </summary>
public interface IUserAccountService
{
    Task<User?> FindByUniversityIdAsync(long universityId, CancellationToken ct);
    Task<User?> FindByIdAsync(int userId, CancellationToken ct);
    Task SetPasswordHashAsync(int userId, string passwordHash, CancellationToken ct);
    Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct);
}
