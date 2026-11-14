using SmartSis.Api.Features.Cart.Interfaces;
using SmartSis.Api.Features.Cart.Models.Dtos;
using SmartSis.Api.Features.Cart.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Cart.Services;

/// <summary>
/// Adding to the cart is allowed outside the windows (students plan ahead); the enrollment rules run when they enroll.
/// Two sections of the same course may sit in the cart (one can be swapped for the other).
/// </summary>
public sealed class CartService(ICartRepository cart, ICurrentUser me, IUnitOfWork uow, TimeProvider clock) : ICartService
{
    public async Task<CartDto> GetAsync(CancellationToken ct)
    {
        var studentId = me.UserId;
        var maxCredits = await cart.GetMaxCreditsAsync(studentId, ct);
        var term = await cart.GetCurrentTermAsync(ct);
        if (term is null) return new CartDto(null, null, "Closed", [], 0, 0, maxCredits);

        var t = term.Value;
        var items = await cart.ListAsync(studentId, t.Id, ct);
        var enrolled = await cart.GetEnrolledCreditsAsync(studentId, t.Id, ct);
        var window = new Term { EnrollStart = t.EnrollStart, EnrollEnd = t.EnrollEnd, AddDropStart = t.AddDropStart, AddDropEnd = t.AddDropEnd }
            .WindowAt(clock.GetUtcNow().UtcDateTime);
        return new CartDto(t.Id, t.Name, window.ToString(), items, items.Sum(i => i.Credits), enrolled, maxCredits);
    }

    public async Task<CartDto> AddAsync(AddToCartRequest request, CancellationToken ct)
    {
        var studentId = me.UserId;
        var section = await cart.GetSectionAsync(request.SectionId, ct) ?? throw NotFoundException.For<Section>(request.SectionId);
        if (!section.TermIsCurrent)
            throw new BusinessRuleException($"{section.Label} isn't offered in the current term.", "not_current_term");
        if (await cart.IsEnrolledInSectionAsync(studentId, section.Id, ct))
            throw new BusinessRuleException($"You're already enrolled in {section.Label}.", "already_enrolled");
        if (await cart.ContainsAsync(studentId, section.Id, ct))
            throw new BusinessRuleException($"{section.Label} is already in your cart.", "already_in_cart"); // rule E7 (also a unique index)

        cart.Add(new CartItem { StudentId = studentId, SectionId = section.Id });
        await uow.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }

    public async Task RemoveAsync(int sectionId, CancellationToken ct)
    {
        var item = await cart.FindAsync(me.UserId, sectionId, ct) ?? throw new NotFoundException("This section isn't in your cart.");
        cart.Remove(item);
        await uow.SaveChangesAsync(ct);
    }

    public async Task RemoveIfPresentAsync(int studentId, int sectionId, CancellationToken ct)
    {
        var item = await cart.FindAsync(studentId, sectionId, ct);
        if (item is not null) cart.Remove(item);
    }
}
