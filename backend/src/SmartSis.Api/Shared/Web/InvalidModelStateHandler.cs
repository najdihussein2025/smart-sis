using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace SmartSis.Api.Shared.Web;

/// <summary>
/// Replaces ASP.NET's automatic <c>[ApiController]</c> 400 (binding / JSON errors that happen before our
/// validators run) with the same ValidationException our validators throw, so ExceptionMiddleware gives
/// every 400 one shape: title "Some fields are not valid.", camelCase field keys, <c>traceId</c>.
/// </summary>
public static class InvalidModelStateHandler
{
    private const string InvalidValue = "The value is not valid.";

    public static IActionResult Throw(ActionContext context) => throw new ValidationException(ToFailures(context));

    internal static List<ValidationFailure> ToFailures(ActionContext context)
    {
        var entries = context.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .Select(e => (Key: ToFieldPath(e.Key), e.Value!.Errors))
            .ToList();

        // Binding a body parameter that failed adds a generic "<parameter> field is required" entry next to
        // the real error. Keep it only when it's the only thing we have to report.
        var parameterNames = context.ActionDescriptor.Parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (entries.Any(e => !parameterNames.Contains(e.Key)))
            entries.RemoveAll(e => parameterNames.Contains(e.Key));

        return entries
            .SelectMany(e => e.Errors.Select(err =>
                new ValidationFailure(e.Key, string.IsNullOrWhiteSpace(err.ErrorMessage) ? InvalidValue : err.ErrorMessage)))
            .ToList();
    }

    /// <summary>"$.majors[0].type" → "majors[0].type", "$" → "". Camel-casing is done by ExceptionMiddleware.</summary>
    internal static string ToFieldPath(string key) =>
        key.StartsWith("$.", StringComparison.Ordinal) ? key[2..]
        : key == "$" ? string.Empty
        : key;
}
