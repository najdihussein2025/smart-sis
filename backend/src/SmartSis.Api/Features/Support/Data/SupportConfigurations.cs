using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Support.Data;

internal sealed class SupportCategoryConfiguration : IEntityTypeConfiguration<SupportCategory>
{
    public void Configure(EntityTypeBuilder<SupportCategory> b)
    {
        b.ToTable("SupportCategories");
        b.Property(x => x.Name).HasMaxLength(100);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
{
    public void Configure(EntityTypeBuilder<SupportRequest> b)
    {
        b.ToTable("SupportRequests");
        b.Property(x => x.Subject).HasMaxLength(200);
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => x.StudentId);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedAdminId).IsRequired(false);
    }
}

internal sealed class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> b)
    {
        b.ToTable("SupportMessages");
        b.Property(x => x.Body).HasColumnType("nvarchar(max)");
        b.HasOne(x => x.Request).WithMany(r => r.Messages).HasForeignKey(x => x.RequestId);
        b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId);
    }
}
