using SmartSis.Api.Features.Cart.Models.Dtos;
using SmartSis.Api.Features.Cart.Models.Entities;

namespace SmartSis.Api.Features.Cart.Interfaces;

/// <summary>The student's cart (PB-04). The student always comes from the token.</summary>
public interface ICartService
{
    Task<CartDto> GetAsync(CancellationToken ct);
    Task<CartDto> AddAsync(AddToCartRequest request, CancellationToken ct);
    Task RemoveAsync(int sectionId, CancellationToken ct);

    /// <summary>
    /// Used by Enrollments after an enroll or swap: takes the section out of the student's cart if it is there.
    /// Does not save; the caller's unit of work commits it.
    /// </summary>
    Task RemoveIfPresentAsync(int studentId, int sectionId, CancellationToken ct);
}

/// <summary>Facts about a section, for adding it to a cart.</summary>
public sealed record CartSectionInfo(int Id, int TermId, bool TermIsCurrent, string Label);

public interface ICartRepository
{
    Task<(int Id, string Name, DateTime EnrollStart, DateTime EnrollEnd, DateTime AddDropStart, DateTime AddDropEnd)?> GetCurrentTermAsync(CancellationToken ct);
    Task<List<CartItemDto>> ListAsync(int studentId, int termId, CancellationToken ct);
    Task<int> GetEnrolledCreditsAsync(int studentId, int termId, CancellationToken ct);
    Task<int> GetMaxCreditsAsync(int studentId, CancellationToken ct);
    Task<CartSectionInfo?> GetSectionAsync(int sectionId, CancellationToken ct);
    Task<bool> ContainsAsync(int studentId, int sectionId, CancellationToken ct);
    Task<bool> IsEnrolledInSectionAsync(int studentId, int sectionId, CancellationToken ct);
    Task<CartItem?> FindAsync(int studentId, int sectionId, CancellationToken ct);
    void Add(CartItem item);
    void Remove(CartItem item);
}
