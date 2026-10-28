using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Enums;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Sections.Models.Entities;

/// <summary>A weekly slot of a section. DayOfWeek: 1 = Monday … 6 = Saturday.</summary>
public class SectionMeeting : BaseEntity
{
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public MeetingType Type { get; set; }

    /// <summary>Rule E5: same day and the time ranges overlap.</summary>
    public bool OverlapsWith(SectionMeeting other) =>
        DayOfWeek == other.DayOfWeek && StartTime < other.EndTime && other.StartTime < EndTime;
}
