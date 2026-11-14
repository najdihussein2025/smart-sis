using FluentValidation;
using SmartSis.Api.Features.Cart.Models.Dtos;

namespace SmartSis.Api.Features.Cart.Validators;

internal sealed class AddToCartRequestValidator : AbstractValidator<AddToCartRequest>
{
    public AddToCartRequestValidator()
    {
        RuleFor(x => x.SectionId).GreaterThan(0).WithMessage("Choose a section.");
    }
}
