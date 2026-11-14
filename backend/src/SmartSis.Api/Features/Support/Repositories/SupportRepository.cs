using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Models;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Support.Repositories;

public sealed class SupportRepository(SisDbContext db) : Repository<SupportRequest>(db), ISupportRepository
{
    /// <summary>Requests with the soft-delete filter off, so deactivated students' requests and their authors still show.</summary>
    private IQueryable<SupportRequest> Requests => Set.AsNoTracking().IgnoreQueryFilters();

    private IQueryable<User> AllUsers => Db.Set<User>().AsNoTracking().IgnoreQueryFilters();

    public async Task<PagedResult<SupportRequestSummaryDto>> ListForStudentAsync(
        int studentId, SupportStatus? status, PageQuery page, CancellationToken ct)
    {
        var q = Requests.Where(r => r.StudentId == studentId);
        if (status is not null) q = q.Where(r => r.Status == status);

        var total = await q.CountAsync(ct);
        var items = await NewestFirst(q).Skip(page.Skip).Take(page.Take)
            .Select(r => new SupportRequestSummaryDto(
                r.Id, r.CategoryId, r.Category.Name, r.Subject, r.Status, r.CreatedAt,
                r.Messages.Max(m => (DateTime?)m.CreatedAt) ?? r.CreatedAt,
                r.Messages.Count))
            .ToListAsync(ct);
        return new PagedResult<SupportRequestSummaryDto>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public async Task<PagedResult<AdminSupportRequestDto>> SearchAsync(
        SupportStatus? status, int? categoryId, int? assignedTo, bool unassignedOnly, string? search, PageQuery page, CancellationToken ct)
    {
        var q = Requests;
        if (status is not null) q = q.Where(r => r.Status == status);
        if (categoryId is not null) q = q.Where(r => r.CategoryId == categoryId);
        if (assignedTo is not null) q = q.Where(r => r.AssignedAdminId == assignedTo);
        if (unassignedOnly) q = q.Where(r => r.AssignedAdminId == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower: SQL Server's collation is case-insensitive anyway, EF InMemory (tests) is not.
            var term = search.Trim().ToLower();
            q = q.Where(r => r.Subject.ToLower().Contains(term)
                             || r.Student.User.FullName.ToLower().Contains(term)
                             || r.Student.User.UniversityId.ToString().Contains(term));
        }

        var users = AllUsers;
        var total = await q.CountAsync(ct);
        var items = await NewestFirst(q).Skip(page.Skip).Take(page.Take)
            .Select(r => new AdminSupportRequestDto(
                r.Id, r.Subject, r.CategoryId, r.Category.Name, r.Status,
                r.StudentId, r.Student.User.UniversityId, r.Student.User.FullName,
                r.AssignedAdminId, users.Where(u => u.Id == r.AssignedAdminId).Select(u => u.FullName).FirstOrDefault(),
                r.CreatedAt,
                r.Messages.Max(m => (DateTime?)m.CreatedAt) ?? r.CreatedAt,
                r.Messages.Count))
            .ToListAsync(ct);
        return new PagedResult<AdminSupportRequestDto>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public async Task<SupportRequestDetailDto?> GetStudentDetailAsync(int id, int studentId, CancellationToken ct)
    {
        var row = await Requests.Where(r => r.Id == id && r.StudentId == studentId)
            .Select(r => new { r.Id, r.CategoryId, Category = r.Category.Name, r.Subject, r.Description, r.Status, r.CreatedAt, r.ResolvedAt })
            .FirstOrDefaultAsync(ct);
        if (row is null) return null;
        return new SupportRequestDetailDto(
            row.Id, row.CategoryId, row.Category, row.Subject, row.Description, row.Status, row.CreatedAt, row.ResolvedAt,
            await MessagesAsync(id, ct));
    }

    public async Task<AdminSupportRequestDetailDto?> GetAdminDetailAsync(int id, CancellationToken ct)
    {
        var users = AllUsers;
        var row = await Requests.Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id, r.Subject, r.Description, r.CategoryId, Category = r.Category.Name, r.Status,
                r.StudentId, r.Student.User.UniversityId, r.Student.User.FullName, r.Student.User.Email,
                r.AssignedAdminId,
                AssignedAdminName = users.Where(u => u.Id == r.AssignedAdminId).Select(u => u.FullName).FirstOrDefault(),
                r.CreatedAt, r.ResolvedAt,
            })
            .FirstOrDefaultAsync(ct);
        if (row is null) return null;
        return new AdminSupportRequestDetailDto(
            row.Id, row.Subject, row.Description, row.CategoryId, row.Category, row.Status,
            row.StudentId, row.UniversityId, row.FullName, row.Email,
            row.AssignedAdminId, row.AssignedAdminName, row.CreatedAt, row.ResolvedAt,
            await MessagesAsync(id, ct));
    }

    public Task<SupportRequest?> GetForUpdateAsync(int id, int? studentId, CancellationToken ct) =>
        Set.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id && (studentId == null || r.StudentId == studentId), ct);

    public async Task<(string Email, string FullName)?> GetContactAsync(int userId, CancellationToken ct)
    {
        var u = await AllUsers.Where(x => x.Id == userId).Select(x => new { x.Email, x.FullName }).FirstOrDefaultAsync(ct);
        return u is null ? null : (u.Email, u.FullName);
    }

    public Task<bool> IsActiveAdminAsync(int userId, CancellationToken ct) =>
        Db.Set<User>().AnyAsync(u => u.Id == userId && u.Role == UserRole.Admin && u.IsActive, ct);

    public Task<List<SupportAdminDto>> ListActiveAdminsAsync(CancellationToken ct) =>
        Db.Set<User>().AsNoTracking()
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new SupportAdminDto(u.Id, u.FullName, u.UniversityId))
            .ToListAsync(ct);

    public Task<List<SupportCategoryDto>> ListCategoriesAsync(CancellationToken ct) =>
        Db.Set<SupportCategory>().AsNoTracking().OrderBy(c => c.Name).Select(c => new SupportCategoryDto(c.Id, c.Name)).ToListAsync(ct);

    public Task<List<AdminSupportCategoryDto>> ListCategoriesWithCountsAsync(CancellationToken ct) =>
        Db.Set<SupportCategory>().AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new AdminSupportCategoryDto(c.Id, c.Name, Set.Count(r => r.CategoryId == c.Id)))
            .ToListAsync(ct);

    public Task<SupportCategory?> FindCategoryAsync(int id, CancellationToken ct) =>
        Db.Set<SupportCategory>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> CategoryNameExistsAsync(string name, int? excludeId, CancellationToken ct)
    {
        var lower = name.ToLower();
        return Db.Set<SupportCategory>().AnyAsync(c => c.Name.ToLower() == lower && (excludeId == null || c.Id != excludeId), ct);
    }

    public Task<int> CountRequestsInCategoryAsync(int categoryId, CancellationToken ct) =>
        Set.IgnoreQueryFilters().CountAsync(r => r.CategoryId == categoryId, ct);

    public void AddCategory(SupportCategory category) => Db.Set<SupportCategory>().Add(category);

    public void RemoveCategory(SupportCategory category) => Db.Set<SupportCategory>().Remove(category);

    private static IOrderedQueryable<SupportRequest> NewestFirst(IQueryable<SupportRequest> q) =>
        q.OrderByDescending(r => r.Messages.Max(m => (DateTime?)m.CreatedAt) ?? r.CreatedAt).ThenByDescending(r => r.Id);

    private Task<List<SupportMessageDto>> MessagesAsync(int requestId, CancellationToken ct) =>
        Db.Set<SupportMessage>().AsNoTracking().IgnoreQueryFilters()
            .Where(m => m.RequestId == requestId)
            .OrderBy(m => m.CreatedAt).ThenBy(m => m.Id)
            .Select(m => new SupportMessageDto(m.Id, m.AuthorId, m.Author.FullName, m.Author.Role, m.Body, m.CreatedAt))
            .ToListAsync(ct);
}
