using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Auth.Models.Entities;

/// <summary>Single-use reset token. Only the SHA-256 hash of the token is stored.</summary>
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsUsable(DateTime utcNow) => UsedAt is null && ExpiresAt > utcNow;

    public void MarkUsed(DateTime utcNow) => UsedAt = utcNow;
}
