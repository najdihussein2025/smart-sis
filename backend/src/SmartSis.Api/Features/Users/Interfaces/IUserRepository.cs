using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Users.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByUniversityIdAsync(long universityId, CancellationToken ct);

    /// <summary>User with Student (Major, Campus) or Instructor (Department) loaded. No tracking.</summary>
    Task<User?> GetWithProfileAsync(int userId, CancellationToken ct);
}
