using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Users.Data;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasIndex(x => x.UniversityId).IsUnique();
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Email).HasMaxLength(256);
        b.Property(x => x.PasswordHash).HasMaxLength(100);
        b.Property(x => x.FullName).HasMaxLength(150);
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Property(x => x.PhotoUrl).HasMaxLength(1024);
    }
}

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.ToTable("Students", t => t.HasCheckConstraint("CK_Students_MaxCreditsOverride", "[MaxCreditsOverride] IS NULL OR [MaxCreditsOverride] > 0"));
        b.HasKey(x => x.UserId);
        b.Property(x => x.UserId).ValueGeneratedNever();
        b.HasOne(x => x.User).WithOne(u => u.Student).HasForeignKey<Student>(x => x.UserId);
        b.HasOne(x => x.Major).WithMany().HasForeignKey(x => x.MajorId);
        b.HasOne(x => x.Campus).WithMany().HasForeignKey(x => x.CampusId);
    }
}

internal sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> b)
    {
        b.ToTable("Instructors");
        b.HasKey(x => x.UserId);
        b.Property(x => x.UserId).ValueGeneratedNever();
        b.HasOne(x => x.User).WithOne(u => u.Instructor).HasForeignKey<Instructor>(x => x.UserId);
        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
    }
}
