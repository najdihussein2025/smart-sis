using System.Globalization;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Api.Features.Attendance.Rules;

/// <summary>
/// Which dates attendance can be taken for (T39): not in the future, inside the section's term, and on the meeting's
/// weekday (1 = Monday … 6 = Saturday). Pure: today is passed in.
/// </summary>
public static class SessionDatePolicy
{
    public const string FutureDate = "future_date";
    public const string OutsideTerm = "outside_term";
    public const string WrongDay = "wrong_day";

    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-GB");

    public static void Check(int meetingDayOfWeek, DateOnly date, DateOnly termStart, DateOnly termEnd, DateOnly today)
    {
        if (date > today)
            throw new BusinessRuleException("Attendance can't be taken for a future date.", FutureDate);

        if (date < termStart || date > termEnd)
            throw new BusinessRuleException(
                $"Choose a date inside the term ({Format(termStart)} to {Format(termEnd)}).", OutsideTerm);

        if ((int)date.DayOfWeek != meetingDayOfWeek)
            throw new BusinessRuleException(
                $"This class meets on {DayName(meetingDayOfWeek)}, but {Format(date)} is a {date.DayOfWeek}.", WrongDay);
    }

    /// <summary>1 = Monday … 6 = Saturday (Sunday = 0 is never a meeting day).</summary>
    public static string DayName(int dayOfWeek) => ((DayOfWeek)(dayOfWeek % 7)).ToString();

    public static string Format(DateOnly d) => d.ToString("dd/MM/yyyy", En);
}
