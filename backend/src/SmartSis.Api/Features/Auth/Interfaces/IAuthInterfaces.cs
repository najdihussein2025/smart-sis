using SmartSis.Api.Features.Auth.Models.Dtos;
using SmartSis.Api.Features.Auth.Models.Entities;

namespace SmartSis.Api.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthUserDto> GetCurrentAsync(CancellationToken ct);

    /// <summary>Always succeeds from the caller's view, so it can't be used to discover valid IDs.</summary>
    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
}

public interface IAuthRepository
{
    void AddResetToken(PasswordResetToken token);
    Task<PasswordResetToken?> FindResetTokenAsync(string tokenHash, CancellationToken ct);
    Task<List<PasswordResetToken>> GetOpenResetTokensAsync(int userId, DateTime utcNow, CancellationToken ct);
}
