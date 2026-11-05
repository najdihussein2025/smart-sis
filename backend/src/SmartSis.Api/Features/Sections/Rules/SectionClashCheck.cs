namespace SmartSis.Api.Features.Sections.Rules;

/// <summary>A weekly slot being saved. <c>DayOfWeek</c>: 1 = Monday … 6 = Saturday.</summary>
public sealed record MeetingSlot(int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int RoomId);

/// <summary>A meeting of another section in the same term, as already stored.</summary>
/// <param name="SectionLabel">For messages, e.g. "CSC 101 A".</param>
/// <param name="InstructorId">That section's instructor, or null when none is assigned.</param>
/// <param name="DayOfWeek">1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">Meeting start.</param>
/// <param name="EndTime">Meeting end.</param>
/// <param name="RoomId">Room id.</param>
/// <param name="RoomName">For messages, e.g. "B-101".</param>
public sealed record ScheduledMeeting(
    string SectionLabel, int? InstructorId, int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int RoomId, string RoomName);

public enum ClashKind
{
    /// <summary>Two meetings of the section being saved overlap each other.</summary>
    SameSection,
    /// <summary>The room is already used by another section at that time.</summary>
    Room,
    /// <summary>The instructor already teaches another section at that time.</summary>
    Instructor,
}

/// <summary>
/// The first clash found. <c>MeetingIndex</c> is the index in the proposed list; <c>OtherIndex</c> is set for
/// <see cref="ClashKind.SameSection"/>, <c>Other</c> for room and instructor clashes.
/// </summary>
public sealed record Clash(ClashKind Kind, int MeetingIndex, int? OtherIndex = null, ScheduledMeeting? Other = null);

/// <summary>
/// UC-16 / SD-16: a section can't be saved if its meetings overlap each other, or overlap another section of the
/// same term in the same room or with the same instructor. Overlap = same day and the time ranges intersect;
/// back-to-back (one ends 09:15, the next starts 09:15) is fine. Pure: the caller passes the other sections' meetings.
/// </summary>
public static class SectionClashCheck
{
    /// <summary>Checks in order: same section, then room, then instructor. Returns null when there is no clash.</summary>
    public static Clash? Find(IReadOnlyList<MeetingSlot> proposed, int? instructorId, IReadOnlyCollection<ScheduledMeeting> others)
    {
        for (var i = 0; i < proposed.Count; i++)
            for (var j = i + 1; j < proposed.Count; j++)
                if (Overlaps(proposed[i], proposed[j].DayOfWeek, proposed[j].StartTime, proposed[j].EndTime))
                    return new Clash(ClashKind.SameSection, i, OtherIndex: j);

        for (var i = 0; i < proposed.Count; i++)
        {
            var m = proposed[i];
            var room = others.FirstOrDefault(o => o.RoomId == m.RoomId && Overlaps(m, o.DayOfWeek, o.StartTime, o.EndTime));
            if (room is not null) return new Clash(ClashKind.Room, i, Other: room);
        }

        if (instructorId is null) return null;

        for (var i = 0; i < proposed.Count; i++)
        {
            var m = proposed[i];
            var teaching = others.FirstOrDefault(o => o.InstructorId == instructorId && Overlaps(m, o.DayOfWeek, o.StartTime, o.EndTime));
            if (teaching is not null) return new Clash(ClashKind.Instructor, i, Other: teaching);
        }

        return null;
    }

    /// <summary>Same rule as <c>SectionMeeting.OverlapsWith</c> (rule E5).</summary>
    public static bool Overlaps(MeetingSlot a, int day, TimeOnly start, TimeOnly end) =>
        a.DayOfWeek == day && a.StartTime < end && start < a.EndTime;
}
