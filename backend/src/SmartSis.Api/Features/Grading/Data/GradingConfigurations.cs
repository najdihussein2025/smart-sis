using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Grading.Data;

internal sealed class GradeComponentConfiguration : IEntityTypeConfiguration<GradeComponent>
{
    public void Configure(EntityTypeBuilder<GradeComponent> b)
    {
        b.ToTable("GradeComponents", t =>
        {
            t.HasCheckConstraint("CK_GradeComponents_Weight", "[Weight] > 0 AND [Weight] <= 100");
            t.HasCheckConstraint("CK_GradeComponents_MaxGrade", "[MaxGrade] > 0");
        });
        b.Property(x => x.Name).HasMaxLength(64);
        b.HasIndex(x => new { x.CourseId, x.TermId, x.Name }).IsUnique();
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
        b.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId);
    }
}

internal sealed class StudentGradeConfiguration : IEntityTypeConfiguration<StudentGrade>
{
    public void Configure(EntityTypeBuilder<StudentGrade> b)
    {
        b.ToTable("StudentGrades", t => t.HasCheckConstraint("CK_StudentGrades_Score", "[Score] >= 0"));
        b.HasIndex(x => new { x.EnrollmentId, x.ComponentId }).IsUnique(); // rule G2
        b.HasOne(x => x.Enrollment).WithMany().HasForeignKey(x => x.EnrollmentId);
        b.HasOne(x => x.Component).WithMany().HasForeignKey(x => x.ComponentId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.GradedBy);
    }
}

internal sealed class TermResultConfiguration : IEntityTypeConfiguration<TermResult>
{
    public void Configure(EntityTypeBuilder<TermResult> b)
    {
        b.ToTable("TermResults");
        b.HasKey(x => new { x.StudentId, x.TermId });
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId);
    }
}
