namespace SmartSis.Api.Shared.Exceptions;

/// <summary>409. A business rule was broken. The message is shown to the user, so write it for humans.</summary>
public sealed class BusinessRuleException(string message, string? code = null) : Exception(message)
{
    /// <summary>Optional machine-readable code (e.g. "schedule_conflict") for the frontend.</summary>
    public string? Code { get; } = code;
}

/// <summary>404.</summary>
public sealed class NotFoundException(string message) : Exception(message)
{
    public static NotFoundException For<T>(object key) => new($"{typeof(T).Name} '{key}' was not found.");
}

/// <summary>403. Signed in but not allowed (wrong role or not the owner).</summary>
public sealed class ForbiddenException(string message = "You are not allowed to do this.") : Exception(message);

/// <summary>401. Bad credentials or no valid session.</summary>
public sealed class UnauthorizedException(string message) : Exception(message);
