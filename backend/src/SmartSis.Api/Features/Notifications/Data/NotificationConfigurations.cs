using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Notifications.Models.Entities;

namespace SmartSis.Api.Features.Notifications.Data;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Body).HasMaxLength(1000);
        b.Property(x => x.Link).HasMaxLength(512);
        b.HasIndex(x => new { x.UserId, x.ReadAt });
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

internal sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> b)
    {
        b.ToTable("EmailOutbox");
        b.Property(x => x.ToEmail).HasMaxLength(256);
        b.Property(x => x.Subject).HasMaxLength(256);
        b.Property(x => x.Body).HasColumnType("nvarchar(max)");
        b.Property(x => x.LastError).HasMaxLength(1000);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
