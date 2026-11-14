using SmartSis.Api.Features.Chat.Interfaces;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Chat.Services;

/// <summary>
/// The JWT the caller sent (already validated by the authentication middleware). The chatbot forwards exactly this token to
/// the AI service, so the AI can never act as anyone but the caller.
/// </summary>
public sealed class BearerTokenAccessor(IHttpContextAccessor accessor) : IBearerTokenAccessor
{
    private const string Prefix = "Bearer ";

    public string GetToken()
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedException("Not signed in.");
        return header[Prefix.Length..].Trim();
    }
}
