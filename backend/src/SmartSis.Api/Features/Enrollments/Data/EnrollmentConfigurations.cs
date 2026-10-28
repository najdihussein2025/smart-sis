using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Enrollments.Data;

internal sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("Enrollments", t => t.HasCheckConstraint("CK_Enrollments_FinalPct", "[FinalPct] IS NULL OR ([FinalPct] >= 0 AND [FinalPct] <= 100)"));
        b.Property(x => x.LetterGrade).HasMaxLength(4);
        b.HasIndex(x => new { x.StudentId, x.Status });
        b.HasIndex(x => new { x.SectionId, x.Status });
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId);
        b.Ignore(x => x.IsActive);
        b.Ignore(x => x.HasPassed);
    }
}

internal sealed class EnrollmentActionConfiguration : IEntityTypeConfiguration<EnrollmentAction>
{
    public void Configure(EntityTypeBuilder<EnrollmentAction> b)
    {
        b.ToTable("EnrollmentActions");
        b.HasIndex(x => new { x.StudentId, x.PerformedAt });
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.HasOne<Section>().WithMany().HasForeignKey(x => x.FromSectionId).IsRequired(false);
        b.HasOne<Section>().WithMany().HasForeignKey(x => x.ToSectionId).IsRequired(false);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.PerformedBy);
    }
}
