using System.Security.Cryptography;
using System.Text;
using SmartSis.Api.Features.Auth.Interfaces;
using SmartSis.Api.Features.Auth.Models.Dtos;
using SmartSis.Api.Features.Auth.Models.Entities;
using SmartSis.Api.Features.Notifications.Interfaces;
using SmartSis.Api.Features.Users.Interfaces;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Auth;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Email;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Auth.Services;

public sealed class AuthService(
    IUserAccountService accounts,
    IAuthRepository repo,
    IPasswordHasher hasher,
    IJwtTokenService tokens,
    IEmailQueue emails,
    IUnitOfWork uow,
    ICurrentUser me,
    TimeProvider clock,
    IConfiguration config) : IAuthService
{
    public const string InvalidCredentials = "Invalid University ID or password.";
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await accounts.FindByUniversityIdAsync(request.UniversityId, ct);

        // Same message for unknown ID, wrong password and deactivated account (security.md).
        if (user is null || !user.CanSignIn || !hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(InvalidCredentials);

        var issued = tokens.Issue(user.Id, user.UniversityId, user.FullName, user.Role.ToString());
        return new LoginResponse(issued.AccessToken, issued.ExpiresAt, ToDto(user));
    }

    public async Task<AuthUserDto> GetCurrentAsync(CancellationToken ct)
    {
        var user = await accounts.FindByIdAsync(me.UserId, ct);
        if (user is null || !user.CanSignIn) throw new UnauthorizedException("Your session is no longer valid.");
        return ToDto(user);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await accounts.FindByUniversityIdAsync(request.UniversityId, ct);
        if (user is null || !user.CanSignIn) return;

        var now = clock.GetUtcNow().UtcDateTime;

        // Only the newest link works.
        foreach (var old in await repo.GetOpenResetTokensAsync(user.Id, now, ct))
            old.MarkUsed(now);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        repo.AddResetToken(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAt = now.Add(ResetTokenLifetime),
        });

        var baseUrl = (config.GetValue<string>("Frontend:BaseUrl") ?? "http://localhost:5173").TrimEnd('/');
        var (subject, body) = EmailTemplates.PasswordReset(user.FullName, $"{baseUrl}/reset-password?token={rawToken}");
        emails.Enqueue(user.Email, subject, body);

        await uow.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var token = await repo.FindResetTokenAsync(Hash(request.Token), ct);
        if (token is null || !token.IsUsable(now))
            throw new BusinessRuleException("This reset link is invalid or has expired. Request a new one.", "reset_token_invalid");

        await accounts.SetPasswordHashAsync(token.UserId, hasher.Hash(request.NewPassword), ct);
        token.MarkUsed(now);
        await uow.SaveChangesAsync(ct);
    }

    internal static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static AuthUserDto ToDto(User u) => new(u.Id, u.UniversityId, u.FullName, u.Role.ToString(), u.PhotoUrl);
}
