using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Announcements.Models.Entities;

namespace SmartSis.Api.Features.Announcements.Data;

internal sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> b)
    {
        b.ToTable("Announcements", t =>
        {
            t.HasCheckConstraint("CK_Announcements_MajorScope", "([Scope] = 'Major' AND [MajorId] IS NOT NULL) OR ([Scope] <> 'Major' AND [MajorId] IS NULL)");
            t.HasCheckConstraint("CK_Announcements_SectionScope", "([Scope] = 'Section' AND [SectionId] IS NOT NULL) OR ([Scope] <> 'Section' AND [SectionId] IS NULL)");
        });
        b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Body).HasColumnType("nvarchar(max)");
        b.HasIndex(x => new { x.Scope, x.PublishAt });
        b.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId);
        b.HasOne(x => x.Major).WithMany().HasForeignKey(x => x.MajorId).IsRequired(false);
        b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId).IsRequired(false);
    }
}
