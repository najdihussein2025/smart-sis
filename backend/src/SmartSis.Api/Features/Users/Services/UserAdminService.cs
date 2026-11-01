using FluentValidation;
using FluentValidation.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Models.Dtos;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Users.Services;

/// <summary>
/// Admin add / edit / deactivate for students and instructors (T09, T10). Admin accounts are visible but not managed here.
/// Duplicate checks include deactivated users: the unique indexes cover every row, the query filter doesn't.
/// </summary>
public sealed class UserAdminService(
    IUserRepository users,
    IPasswordHasher hasher,
    IUnitOfWork uow,
    ICurrentUser me,
    TimeProvider clock) : IUserAdminService
{
    public Task<PagedResult<UserListItemDto>> SearchAsync(UserSearchQuery query, CancellationToken ct) =>
        users.SearchAsync(query, new PageQuery(query.Page, query.PageSize), ct);

    public async Task<UserDetailDto> GetAsync(int userId, CancellationToken ct) =>
        await users.GetDetailAsync(userId, ct) ?? throw NotFoundException.For<User>(userId);

    public async Task<UserDetailDto> CreateAsync(CreateUserRequest r, CancellationToken ct)
    {
        var email = NormalizeEmail(r.Email);
        await EnsureProfileReferencesExistAsync(r.Student, r.Instructor, ct);

        if (await users.UniversityIdExistsAsync(r.UniversityId, ct))
            throw DuplicateUniversityId(r.UniversityId);
        if (await users.EmailExistsAsync(email, excludeUserId: null, ct))
            throw DuplicateEmail(email);

        var user = new User
        {
            UniversityId = r.UniversityId,
            Email = email,
            FullName = r.FullName.Trim(),
            Phone = Clean(r.Phone),
            DateOfBirth = r.DateOfBirth,
            Role = r.Role,
            PasswordHash = hasher.Hash(r.InitialPassword),
            IsActive = true,
        };
        if (r.Role == UserRole.Student) user.Student = NewStudent(r.Student!);
        else user.Instructor = new Instructor { DepartmentId = r.Instructor!.DepartmentId };

        // User + Student/Instructor row are saved together.
        await SaveTranslatingDuplicatesAsync(() => uow.ExecuteInTransactionAsync(() =>
        {
            users.Add(user);
            return Task.CompletedTask;
        }, ct), r.UniversityId, email);

        return await GetAsync(user.Id, ct);
    }

    public async Task<UserDetailDto> UpdateAsync(int userId, UpdateUserRequest r, CancellationToken ct)
    {
        var user = await users.GetForAdminAsync(userId, ct) ?? throw NotFoundException.For<User>(userId);
        EnsureManaged(user);
        if (!user.CanSignIn)
            throw new BusinessRuleException("This account is deactivated.", "user_inactive");

        EnsureProfileMatchesRole(user.Role, r.Student, r.Instructor);
        await EnsureProfileReferencesExistAsync(r.Student, r.Instructor, ct);

        var email = NormalizeEmail(r.Email);
        if (await users.EmailExistsAsync(email, excludeUserId: user.Id, ct))
            throw DuplicateEmail(email);

        user.Email = email;
        user.FullName = r.FullName.Trim();
        user.Phone = Clean(r.Phone);
        user.DateOfBirth = r.DateOfBirth;
        if (user.Role == UserRole.Student)
        {
            var s = user.Student ??= new Student { UserId = user.Id };
            s.MajorId = r.Student!.MajorId;
            s.CampusId = r.Student.CampusId;
            s.EnrollmentDate = r.Student.EnrollmentDate;
            s.MaxCreditsOverride = r.Student.MaxCreditsOverride;
        }
        else
        {
            var i = user.Instructor ??= new Instructor { UserId = user.Id };
            i.DepartmentId = r.Instructor!.DepartmentId;
        }

        await SaveTranslatingDuplicatesAsync(() => uow.SaveChangesAsync(ct), user.UniversityId, email);
        return await GetAsync(user.Id, ct);
    }

    public async Task DeactivateAsync(int userId, CancellationToken ct)
    {
        var user = await users.GetForAdminAsync(userId, ct) ?? throw NotFoundException.For<User>(userId);
        if (user.Id == me.UserId)
            throw new BusinessRuleException("You can't deactivate your own account.", "cannot_deactivate_self");
        EnsureManaged(user);
        if (!user.CanSignIn) return; // already deactivated: nothing to do

        // Soft delete (D1). Login already refuses !CanSignIn and the query filter hides the user from other features.
        user.MarkDeleted(clock.GetUtcNow().UtcDateTime);
        await uow.SaveChangesAsync(ct);
    }

    private static void EnsureManaged(User user)
    {
        if (user.Role == UserRole.Admin)
            throw new BusinessRuleException("Admin accounts can't be edited or deactivated here.", "admin_not_managed");
    }

    private static void EnsureProfileMatchesRole(UserRole role, StudentInfoRequest? student, InstructorInfoRequest? instructor)
    {
        var failures = new List<ValidationFailure>();
        if (role == UserRole.Student)
        {
            if (student is null) failures.Add(new("student", "Student details are required for a student."));
            if (instructor is not null) failures.Add(new("instructor", "Leave instructor details empty for a student."));
        }
        else
        {
            if (instructor is null) failures.Add(new("instructor", "Instructor details are required for an instructor."));
            if (student is not null) failures.Add(new("student", "Leave student details empty for an instructor."));
        }
        if (failures.Count > 0) throw new ValidationException(failures);
    }

    private async Task EnsureProfileReferencesExistAsync(StudentInfoRequest? student, InstructorInfoRequest? instructor, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        if (student is not null)
        {
            if (!await users.MajorExistsAsync(student.MajorId, ct))
                failures.Add(new("student.majorId", $"Major {student.MajorId} doesn't exist."));
            if (!await users.CampusExistsAsync(student.CampusId, ct))
                failures.Add(new("student.campusId", $"Campus {student.CampusId} doesn't exist."));
        }
        if (instructor is not null && !await users.DepartmentExistsAsync(instructor.DepartmentId, ct))
            failures.Add(new("instructor.departmentId", $"Department {instructor.DepartmentId} doesn't exist."));
        if (failures.Count > 0) throw new ValidationException(failures);
    }

    /// <summary>
    /// Backstop for races between the duplicate check and the insert: SQL Server unique-index errors (2601 / 2627)
    /// become the same 409 the checks give. EF InMemory never raises these.
    /// </summary>
    private static async Task SaveTranslatingDuplicatesAsync(Func<Task> save, long universityId, string email)
    {
        try
        {
            await save();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 } sql)
        {
            if (sql.Message.Contains("UniversityId", StringComparison.OrdinalIgnoreCase)) throw DuplicateUniversityId(universityId);
            if (sql.Message.Contains("Email", StringComparison.OrdinalIgnoreCase)) throw DuplicateEmail(email);
            throw;
        }
    }

    private static Student NewStudent(StudentInfoRequest s) => new()
    {
        MajorId = s.MajorId,
        CampusId = s.CampusId,
        EnrollmentDate = s.EnrollmentDate,
        MaxCreditsOverride = s.MaxCreditsOverride,
    };

    private static BusinessRuleException DuplicateUniversityId(long id) =>
        new($"University ID {id} is already in use.", "duplicate_university_id");

    private static BusinessRuleException DuplicateEmail(string email) =>
        new($"Email {email} is already in use.", "duplicate_email");

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
