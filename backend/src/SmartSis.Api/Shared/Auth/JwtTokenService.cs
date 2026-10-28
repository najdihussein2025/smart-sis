using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SmartSis.Api.Shared.Auth;

public sealed record IssuedToken(string AccessToken, DateTime ExpiresAt);

public interface IJwtTokenService
{
    IssuedToken Issue(int userId, long universityId, string fullName, string role);
}

public sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider clock) : IJwtTokenService
{
    private readonly JwtSettings _settings = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedToken Issue(int userId, long universityId, string fullName, string role)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.ExpiresMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(SisClaims.UserId, userId.ToString()),
                new Claim(SisClaims.UniversityId, universityId.ToString()),
                new Claim(SisClaims.Name, fullName),
                new Claim(SisClaims.Role, role),
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256),
        };

        return new IssuedToken(_handler.CreateToken(descriptor), expires);
    }
}
