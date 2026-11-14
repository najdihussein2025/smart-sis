using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSis.Api.Features.Cart.Interfaces;
using SmartSis.Api.Features.Cart.Models.Dtos;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Cart.Controllers;

/// <summary>Student: the cart of sections to enroll in (PB-04).</summary>
[ApiController]
[Authorize(Policy = Policies.Student)]
[Route("api/cart")]
[Tags("Student: Cart")]
public sealed class CartController(ICartService cart) : ControllerBase
{
    /// <summary>The cart for the current term, with credits in the cart, credits enrolled and the credit cap.</summary>
    [HttpGet]
    public Task<CartDto> Get(CancellationToken ct) => cart.GetAsync(ct);

    /// <summary>Add a section of the current term.</summary>
    /// <response code="404">No section with this id.</response>
    /// <response code="409"><c>already_in_cart</c>, <c>already_enrolled</c> or <c>not_current_term</c>.</response>
    [HttpPost]
    [ProducesResponseType<CartDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<CartDto> Add(AddToCartRequest request, CancellationToken ct) => cart.AddAsync(request, ct);

    /// <summary>Remove a section from the cart.</summary>
    /// <response code="404">The section isn't in the cart.</response>
    [HttpDelete("{sectionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int sectionId, CancellationToken ct)
    {
        await cart.RemoveAsync(sectionId, ct);
        return NoContent();
    }
}
