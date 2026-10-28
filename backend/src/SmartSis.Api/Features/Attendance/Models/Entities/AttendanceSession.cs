using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Attendance.Models.Entities;

/// <summary>One taken attendance for a meeting on a date. Unique (MeetingId, SessionDate) blocks duplicates (rule A4, bug BG_06).</summary>
public class AttendanceSession : BaseEntity
{
    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
    public int MeetingId { get; set; }
    public SectionMeeting Meeting { get; set; } = null!;
    public DateOnly SessionDate { get; set; }

    /// <summary>Users.Id of the instructor.</summary>
    public int SubmittedBy { get; set; }
    public DateTime SubmittedAt { get; set; }

    public ICollection<AttendanceRecord> Records { get; set; } = [];
}
