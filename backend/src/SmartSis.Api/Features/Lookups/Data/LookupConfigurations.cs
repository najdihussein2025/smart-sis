using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Lookups.Models.Entities;

namespace SmartSis.Api.Features.Lookups.Data;

internal sealed class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> b)
    {
        b.ToTable("Campuses");
        b.Property(x => x.Name).HasMaxLength(100);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("Departments");
        b.Property(x => x.Code).HasMaxLength(16);
        b.Property(x => x.Name).HasMaxLength(150);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class MajorConfiguration : IEntityTypeConfiguration<Major>
{
    public void Configure(EntityTypeBuilder<Major> b)
    {
        b.ToTable("Majors", t => t.HasCheckConstraint("CK_Majors_DefaultMaxCredits", "[DefaultMaxCredits] > 0"));
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.DefaultMaxCredits).HasDefaultValue(18);
        b.HasIndex(x => new { x.DepartmentId, x.Name }).IsUnique();
        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
    }
}

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("Rooms", t => t.HasCheckConstraint("CK_Rooms_Capacity", "[Capacity] > 0"));
        b.Property(x => x.Name).HasMaxLength(50);
        b.HasIndex(x => new { x.CampusId, x.Name }).IsUnique();
        b.HasOne(x => x.Campus).WithMany().HasForeignKey(x => x.CampusId);
    }
}

internal sealed class PaymentTypeConfiguration : IEntityTypeConfiguration<PaymentType>
{
    public void Configure(EntityTypeBuilder<PaymentType> b)
    {
        b.ToTable("PaymentTypes");
        b.Property(x => x.Name).HasMaxLength(100);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class GradeScaleConfiguration : IEntityTypeConfiguration<GradeScale>
{
    private static readonly DateTime Seeded = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<GradeScale> b)
    {
        b.ToTable("GradeScales", t => t.HasCheckConstraint("CK_GradeScales_Range", "[MinPct] >= 0 AND [MaxPct] <= 100 AND [MinPct] <= [MaxPct]"));
        b.Property(x => x.Letter).HasMaxLength(4);
        b.Property(x => x.StatusLabel).HasMaxLength(32);
        b.HasIndex(x => x.Letter).IsUnique();

        // PROJECT.md §4 grade scale. Reference data, never edited at runtime.
        b.HasData(
            Row(1, "A+", "Outstanding", 4.0m, 97, 100),
            Row(2, "A", "Excellent", 4.0m, 93, 96),
            Row(3, "A-", "Very Good", 3.7m, 89, 92),
            Row(4, "B+", "Good", 3.3m, 85, 88),
            Row(5, "B", "Good", 3.0m, 80, 84),
            Row(6, "B-", "Good", 2.7m, 77, 79),
            Row(7, "C+", "Satisfactory", 2.3m, 73, 76),
            Row(8, "C", "Satisfactory", 2.0m, 70, 72),
            Row(9, "C-", "Satisfactory", 1.7m, 66, 69),
            Row(10, "D+", "Passing", 1.3m, 63, 65),
            Row(11, "D", "Passing", 1.0m, 60, 62),
            Row(12, "F", "Failure", 0.0m, 0, 59));
    }

    private static GradeScale Row(int id, string letter, string label, decimal gpa, int min, int max) => new()
    {
        Id = id, Letter = letter, StatusLabel = label, GpaPoints = gpa, MinPct = min, MaxPct = max,
        CreatedAt = Seeded, UpdatedAt = Seeded,
    };
}
