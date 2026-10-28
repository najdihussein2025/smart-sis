using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Auth.Models.Entities;

namespace SmartSis.Api.Features.Auth.Data;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("PasswordResetTokens");
        b.Property(x => x.TokenHash).HasMaxLength(64).IsFixedLength();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}
