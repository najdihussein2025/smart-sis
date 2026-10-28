using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Auth.Interfaces;
using SmartSis.Api.Features.Auth.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Auth.Repositories;

public sealed class AuthRepository(SisDbContext db) : IAuthRepository
{
    private DbSet<PasswordResetToken> Tokens => db.Set<PasswordResetToken>();

    public void AddResetToken(PasswordResetToken token) => Tokens.Add(token);

    public Task<PasswordResetToken?> FindResetTokenAsync(string tokenHash, CancellationToken ct) =>
        Tokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<List<PasswordResetToken>> GetOpenResetTokensAsync(int userId, DateTime utcNow, CancellationToken ct) =>
        Tokens.Where(t => t.UserId == userId && t.UsedAt == null && t.ExpiresAt > utcNow).ToListAsync(ct);
}
