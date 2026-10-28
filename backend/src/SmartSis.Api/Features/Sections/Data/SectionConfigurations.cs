using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Sections.Models.Entities;

namespace SmartSis.Api.Features.Sections.Data;

internal sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> b)
    {
        b.ToTable("Sections", t => t.HasCheckConstraint("CK_Sections_Capacity", "[Capacity] > 0"));
        b.Property(x => x.SectionCode).HasMaxLength(8);
        b.HasIndex(x => new { x.CourseId, x.TermId, x.SectionCode }).IsUnique();
        b.HasIndex(x => new { x.TermId, x.InstructorId });
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId);
        b.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId);
        b.HasOne(x => x.Instructor).WithMany().HasForeignKey(x => x.InstructorId).IsRequired(false);
    }
}

internal sealed class SectionMeetingConfiguration : IEntityTypeConfiguration<SectionMeeting>
{
    public void Configure(EntityTypeBuilder<SectionMeeting> b)
    {
        b.ToTable("SectionMeetings", t =>
        {
            t.HasCheckConstraint("CK_SectionMeetings_Day", "[DayOfWeek] BETWEEN 1 AND 6");
            t.HasCheckConstraint("CK_SectionMeetings_Time", "[StartTime] < [EndTime]");
        });
        b.HasOne(x => x.Section).WithMany(s => s.Meetings).HasForeignKey(x => x.SectionId);
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);
        b.HasIndex(x => new { x.RoomId, x.DayOfWeek });
    }
}
