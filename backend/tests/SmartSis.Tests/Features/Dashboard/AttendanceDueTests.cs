using SmartSis.Api.Features.Dashboard.Rules;

namespace SmartSis.Tests.Features.Dashboard;

/// <summary>PB-22: attendance sessions still to take this week, for the instructor dashboard.</summary>
public sealed class AttendanceDueTests
{
    // 2026-11-09 is a Monday, 2026-11-11 a Wednesday, 2026-11-14 a Saturday, 2026-11-15 a Sunday.
    private static readonly DateOnly Monday = new(2026, 11, 9);
    private static readonly DateOnly Wednesday = new(2026, 11, 11);
    private static readonly DateOnly Saturday = new(2026, 11, 14);
    private static readonly DateOnly Sunday = new(2026, 11, 15);
    private static readonly DateOnly TermStart = new(2026, 9, 1);
    private static readonly DateOnly TermEnd = new(2027, 1, 31);

    private static readonly IReadOnlySet<(int, DateOnly)> NoneTaken = new HashSet<(int, DateOnly)>();

    private static WeeklyMeeting Meeting(int id, int day, DateOnly? start = null, DateOnly? end = null) =>
        new(id, day, start ?? TermStart, end ?? TermEnd);

    [Theory]
    [InlineData("2026-11-09", "2026-11-09")]
    [InlineData("2026-11-11", "2026-11-09")]
    [InlineData("2026-11-14", "2026-11-09")]
    [InlineData("2026-11-15", "2026-11-09")]
    [InlineData("2026-11-16", "2026-11-16")]
    public void WeekStart_IsTheMondayOfTheWeek(string today, string monday) =>
        Assert.Equal(DateOnly.Parse(monday), AttendanceDue.WeekStart(DateOnly.Parse(today)));

    [Fact]
    public void Count_MeetingsUpToToday_AreDue_LaterOnesAreNot()
    {
        // Monday and Wednesday meetings; on Wednesday both have happened, Friday's hasn't.
        var meetings = new[] { Meeting(1, 1), Meeting(2, 3), Meeting(3, 5) };
        Assert.Equal(2, AttendanceDue.Count(meetings, NoneTaken, Wednesday));
    }

    [Fact]
    public void Count_OnMonday_OnlyMondayIsDue()
    {
        var meetings = new[] { Meeting(1, 1), Meeting(2, 3) };
        Assert.Equal(1, AttendanceDue.Count(meetings, NoneTaken, Monday));
    }

    [Fact]
    public void Count_TakenSessions_AreNotDue()
    {
        var meetings = new[] { Meeting(1, 1), Meeting(2, 3) };
        var taken = new HashSet<(int, DateOnly)> { (1, Monday) };
        Assert.Equal(1, AttendanceDue.Count(meetings, taken, Wednesday));
    }

    [Fact]
    public void Count_SessionOnAnotherDate_DoesNotCount()
    {
        var meetings = new[] { Meeting(1, 1) };
        var taken = new HashSet<(int, DateOnly)> { (1, Monday.AddDays(-7)) };
        Assert.Equal(1, AttendanceDue.Count(meetings, taken, Wednesday));
    }

    [Fact]
    public void Count_OnSunday_TheWholeWeekCounts()
    {
        var meetings = new[] { Meeting(1, 1), Meeting(2, 3), Meeting(3, 6) };
        Assert.Equal(3, AttendanceDue.Count(meetings, NoneTaken, Sunday));
        Assert.Equal(3, AttendanceDue.Count(meetings, NoneTaken, Saturday));
    }

    [Fact]
    public void Count_DatesOutsideTheTerm_AreNotDue()
    {
        // Term starts on the Wednesday: Monday's meeting this week is before the term.
        var meetings = new[] { Meeting(1, 1, start: Wednesday), Meeting(2, 3, start: Wednesday), Meeting(3, 5, end: Wednesday) };
        Assert.Equal(1, AttendanceDue.Count(meetings, NoneTaken, Saturday));
    }

    [Fact]
    public void Count_NoMeetings_Zero() => Assert.Equal(0, AttendanceDue.Count([], NoneTaken, Saturday));
}
