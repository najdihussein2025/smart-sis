using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Users.Services;

public sealed class UserAccountService(IUserRepository users) : IUserAccountService
{
    public Task<User?> FindByUniversityIdAsync(long universityId, CancellationToken ct) =>
        users.FindByUniversityIdAsync(universityId, ct);

    public Task<User?> FindByIdAsync(int userId, CancellationToken ct) => users.FindAsync(userId, ct);

    public async Task SetPasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
    {
        var user = await users.FindAsync(userId, ct) ?? throw NotFoundException.For<User>(userId);
        user.PasswordHash = passwordHash;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct)
    {
        var u = await users.GetWithProfileAsync(userId, ct) ?? throw NotFoundException.For<User>(userId);

        return new UserProfileDto(
            u.Id, u.UniversityId, u.FullName, u.Email, u.Role.ToString(), u.Phone, u.DateOfBirth, u.PhotoUrl,
            u.Student is { } s
                ? new StudentProfileDto(s.MajorId, s.Major.Name, s.Campus.Name, s.EnrollmentDate, s.EffectiveMaxCredits(s.Major.DefaultMaxCredits))
                : null,
            u.Instructor is { } i
                ? new InstructorProfileDto(i.DepartmentId, i.Department.Name)
                : null);
    }
}
