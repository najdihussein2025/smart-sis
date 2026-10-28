using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SmartSis.Api.Shared.Web;

/// <summary>
/// Runs the FluentValidation validator for every action argument that has one.
/// Failures throw ValidationException, which ExceptionMiddleware turns into a 400.
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid) throw new ValidationException(result.Errors);
        }

        await next();
    }
}
