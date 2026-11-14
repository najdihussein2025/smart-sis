using SmartSis.Api.Features.Support.Interfaces;
using SmartSis.Api.Features.Support.Models.Dtos;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Support.Services;

/// <summary>
/// Names are unique, ignoring case (409 <c>duplicate_category</c>). A category used by any request can't be deleted
/// (409 <c>category_in_use</c>) — rename it instead. An unused one is removed for good: nothing points at it.
/// </summary>
public sealed class SupportCategoryService(ISupportRepository repo, IUnitOfWork uow) : ISupportCategoryService
{
    public Task<List<AdminSupportCategoryDto>> ListAsync(CancellationToken ct) => repo.ListCategoriesWithCountsAsync(ct);

    public async Task<AdminSupportCategoryDto> CreateAsync(SaveSupportCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, null, ct);
        var category = new SupportCategory { Name = name };
        repo.AddCategory(category);
        await uow.SaveChangesAsync(ct);
        return new AdminSupportCategoryDto(category.Id, category.Name, 0);
    }

    public async Task<AdminSupportCategoryDto> RenameAsync(int id, SaveSupportCategoryRequest request, CancellationToken ct)
    {
        var category = await repo.FindCategoryAsync(id, ct) ?? throw NotFound(id);
        var name = request.Name.Trim();
        await EnsureNameFreeAsync(name, id, ct);
        category.Name = name;
        await uow.SaveChangesAsync(ct);
        return new AdminSupportCategoryDto(category.Id, category.Name, await repo.CountRequestsInCategoryAsync(id, ct));
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var category = await repo.FindCategoryAsync(id, ct) ?? throw NotFound(id);
        var used = await repo.CountRequestsInCategoryAsync(id, ct);
        if (used > 0)
            throw new BusinessRuleException(
                $"\"{category.Name}\" is used by {used} support request{(used == 1 ? "" : "s")}, so it can't be deleted. Rename it instead.",
                "category_in_use");
        repo.RemoveCategory(category);
        await uow.SaveChangesAsync(ct);
    }

    private async Task EnsureNameFreeAsync(string name, int? excludeId, CancellationToken ct)
    {
        if (await repo.CategoryNameExistsAsync(name, excludeId, ct))
            throw new BusinessRuleException($"A category named \"{name}\" already exists.", "duplicate_category");
    }

    private static NotFoundException NotFound(int id) => new($"Support category {id} was not found.");
}
