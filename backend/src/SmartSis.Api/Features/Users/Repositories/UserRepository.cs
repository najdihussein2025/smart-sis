using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Models;
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

    public async Task<PagedResult<UserListItemDto>> SearchAsync(UserSearchQuery query, PageQuery page, CancellationToken ct)
    {
        var q = Set.IgnoreQueryFilters().AsNoTracking();

        q = query.Status switch
        {
            UserStatusFilter.Active => q.Where(u => u.IsActive && u.DeletedAt == null),
            UserStatusFilter.Inactive => q.Where(u => !u.IsActive || u.DeletedAt != null),
            _ => q,
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // ToLower: SQL Server's collation is case-insensitive anyway, EF InMemory (tests) is not.
            var s = query.Search.Trim().ToLower();
            q = q.Where(u => u.FullName.ToLower().Contains(s) || u.Email.ToLower().Contains(s) || u.UniversityId.ToString().Contains(s));
        }
        if (query.Role is not null)
            q = q.Where(u => u.Role == query.Role);
        if (query.MajorId is not null)
            q = q.Where(u => u.Student != null && u.Student.MajorId == query.MajorId);
        if (query.DepartmentId is not null)
            q = q.Where(u => (u.Instructor != null && u.Instructor.DepartmentId == query.DepartmentId)
                          || (u.Student != null && u.Student.Major.DepartmentId == query.DepartmentId));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(u => u.FullName).ThenBy(u => u.UniversityId)
            .Skip(page.Skip).Take(page.Take)
            .Select(u => new UserListItemDto(
                u.Id, u.UniversityId, u.FullName, u.Email, u.Role, u.IsActive && u.DeletedAt == null,
                u.Student != null ? u.Student.Major.Name : null,
                u.Instructor != null ? u.Instructor.Department.Name : null))
            .ToListAsync(ct);
        return new PagedResult<UserListItemDto>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public Task<UserDetailDto?> GetDetailAsync(int userId, CancellationToken ct) =>
        Set.IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserDetailDto(
                u.Id, u.UniversityId, u.FullName, u.Email, u.Role, u.Phone, u.DateOfBirth, u.PhotoUrl,
                u.IsActive && u.DeletedAt == null, u.DeletedAt,
                u.Student == null ? null : new UserStudentDto(
                    u.Student.MajorId, u.Student.Major.Name, u.Student.CampusId, u.Student.Campus.Name,
                    u.Student.EnrollmentDate, u.Student.MaxCreditsOverride,
                    // Rule E2, same as Student.EffectiveMaxCredits (inlined so it translates to SQL).
                    u.Student.MaxCreditsOverride ?? u.Student.Major.DefaultMaxCredits),
                u.Instructor == null ? null : new UserInstructorDto(u.Instructor.DepartmentId, u.Instructor.Department.Name)))
            .FirstOrDefaultAsync(ct);

    public Task<User?> GetForAdminAsync(int userId, CancellationToken ct) =>
        Set.IgnoreQueryFilters()
            .Include(u => u.Student)
            .Include(u => u.Instructor)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public Task<bool> UniversityIdExistsAsync(long universityId, CancellationToken ct) =>
        Set.IgnoreQueryFilters().AnyAsync(u => u.UniversityId == universityId, ct);

    public Task<bool> EmailExistsAsync(string email, int? excludeUserId, CancellationToken ct)
    {
        var lower = email.ToLower();
        return Set.IgnoreQueryFilters().AnyAsync(u => u.Email.ToLower() == lower && (excludeUserId == null || u.Id != excludeUserId), ct);
    }

    public Task<bool> MajorExistsAsync(int majorId, CancellationToken ct) => Db.Set<Major>().AnyAsync(m => m.Id == majorId, ct);
    public Task<bool> CampusExistsAsync(int campusId, CancellationToken ct) => Db.Set<Campus>().AnyAsync(c => c.Id == campusId, ct);
    public Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken ct) => Db.Set<Department>().AnyAsync(d => d.Id == departmentId, ct);
}
