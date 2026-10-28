using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Users.Repositories;

public sealed class UserRepository(SisDbContext db) : Repository<User>(db), IUserRepository
{
    public Task<User?> FindByUniversityIdAsync(long universityId, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(u => u.UniversityId == universityId, ct);

    public Task<User?> GetWithProfileAsync(int userId, CancellationToken ct) =>
        Set.AsNoTracking()
            .Include(u => u.Student!).ThenInclude(s => s.Major)
            .Include(u => u.Student!).ThenInclude(s => s.Campus)
            .Include(u => u.Instructor!).ThenInclude(i => i.Department)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
}
