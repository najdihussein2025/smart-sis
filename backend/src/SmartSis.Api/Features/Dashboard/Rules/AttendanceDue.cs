namespace SmartSis.Api.Features.Dashboard.Rules;

/// <summary>A weekly meeting of a section, with the dates of the section's term.</summary>
public sealed record WeeklyMeeting(int MeetingId, int DayOfWeek, DateOnly TermStart, DateOnly TermEnd);

/// <summary>
/// Which attendance sessions are still to be taken this week, for the instructor dashboard. Pure: today is passed in.
/// The week starts on Monday. A meeting is due on each date from Monday up to and including today that falls on its
/// weekday (1 = Monday … 6 = Saturday) and inside its term, unless a session exists for that meeting and date (rule A4).
/// </summary>
public static class AttendanceDue
{
    /// <summary>Monday of the week that contains <paramref name="today"/> (Sunday belongs to the week that started six days before).</summary>
    public static DateOnly WeekStart(DateOnly today) =>
        today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

    public static int Count(IEnumerable<WeeklyMeeting> meetings, IReadOnlySet<(int MeetingId, DateOnly Date)> taken, DateOnly today)
    {
        var monday = WeekStart(today);
        var due = 0;
        foreach (var m in meetings)
        {
            for (var d = monday; d <= today; d = d.AddDays(1))
            {
                if ((int)d.DayOfWeek != m.DayOfWeek) continue;
                if (d < m.TermStart || d > m.TermEnd) continue;
                if (!taken.Contains((m.MeetingId, d))) due++;
            }
        }
        return due;
    }
}
