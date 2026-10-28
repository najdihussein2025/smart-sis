using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace SmartSis.Api.Shared.Exceptions;

/// <summary>Maps exceptions to ProblemDetails: 400 validation, 401, 403, 404, 409 business rule, 500.</summary>
public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception ex)
    {
        ProblemDetails problem = ex switch
        {
            ValidationException v => new ValidationProblemDetails(
                v.Errors
                    .GroupBy(e => ToCamel(e.PropertyName))
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Some fields are not valid.",
            },
            UnauthorizedException => new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = ex.Message },
            ForbiddenException => new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = ex.Message },
            NotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = ex.Message },
            BusinessRuleException b => WithCode(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = b.Message }, b.Code),
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Something went wrong.",
                Detail = env.IsDevelopment() ? ex.ToString() : null,
            },
        };

        if (problem.Status >= 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = problem.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json");
    }

    private static ProblemDetails WithCode(ProblemDetails p, string? code)
    {
        if (code is not null) p.Extensions["code"] = code;
        return p;
    }

    private static string ToCamel(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
