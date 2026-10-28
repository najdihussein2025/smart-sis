using System.Security.Claims;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Shared.Auth;

/// <summary>The signed-in user, read from the JWT. Identity always comes from here, never from the request body.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    long UniversityId { get; }
    string Role { get; }
    bool IsInRole(string role);
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int UserId => int.TryParse(Principal?.FindFirstValue(SisClaims.UserId), out var id)
        ? id
        : throw new UnauthorizedException("Not signed in.");

    public long UniversityId => long.TryParse(Principal?.FindFirstValue(SisClaims.UniversityId), out var id)
        ? id
        : throw new UnauthorizedException("Not signed in.");

    public string Role => Principal?.FindFirstValue(SisClaims.Role) ?? throw new UnauthorizedException("Not signed in.");

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}
