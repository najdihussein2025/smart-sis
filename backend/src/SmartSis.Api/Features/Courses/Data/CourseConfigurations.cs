using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Courses.Models.Entities;

namespace SmartSis.Api.Features.Courses.Data;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.ToTable("Courses", t => t.HasCheckConstraint("CK_Courses_Credits", "[Credits] BETWEEN 0 AND 12"));
        b.Property(x => x.Code).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Description).HasColumnType("nvarchar(max)");
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        b.Ignore(x => x.AbsenceLimit);
    }
}

internal sealed class MajorCourseConfiguration : IEntityTypeConfiguration<MajorCourse>
{
    public void Configure(EntityTypeBuilder<MajorCourse> b)
    {
        b.ToTable("MajorCourses");
        b.HasKey(x => new { x.MajorId, x.CourseId });
        b.HasOne(x => x.Major).WithMany().HasForeignKey(x => x.MajorId);
        b.HasOne(x => x.Course).WithMany(c => c.MajorCourses).HasForeignKey(x => x.CourseId);
    }
}

internal sealed class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> b)
    {
        b.ToTable("CoursePrerequisites", t => t.HasCheckConstraint("CK_CoursePrerequisites_NotSelf", "[CourseId] <> [PrerequisiteId]"));
        b.HasKey(x => new { x.CourseId, x.PrerequisiteId });
        b.HasOne(x => x.Course).WithMany(c => c.Prerequisites).HasForeignKey(x => x.CourseId);
        b.HasOne(x => x.Prerequisite).WithMany().HasForeignKey(x => x.PrerequisiteId);
    }
}
