using SmartSis.Api.Features.Attendance.Rules;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>T39: attendance dates are on the meeting's weekday, not in the future and inside the term.</summary>
public sealed class SessionDatePolicyTests
{
    // Monday 2 November 2026; the term runs September to January.
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateOnly TermStart = new(2026, 9, 1);
    private static readonly DateOnly TermEnd = new(2027, 1, 29);

    private static string Code(Action act) => Assert.Throws<BusinessRuleException>(act).Code!;

    [Fact]
    public void Check_MeetingDayInThePast_Passes() => SessionDatePolicy.Check(1, Monday, TermStart, TermEnd, today: Monday.AddDays(3));

    [Fact]
    public void Check_Today_Passes() => SessionDatePolicy.Check(1, Monday, TermStart, TermEnd, today: Monday);

    [Fact]
    public void Check_Tomorrow_Throws() =>
        Assert.Equal(SessionDatePolicy.FutureDate, Code(() => SessionDatePolicy.Check(2, Monday.AddDays(1), TermStart, TermEnd, today: Monday)));

    [Fact]
    public void Check_WrongWeekday_Throws_AndNamesBothDays()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => SessionDatePolicy.Check(3, Monday, TermStart, TermEnd, today: Monday));
        Assert.Equal(SessionDatePolicy.WrongDay, ex.Code);
        Assert.Equal("This class meets on Wednesday, but 02/11/2026 is a Monday.", ex.Message);
    }

    [Fact]
    public void Check_Saturday_MatchesDay6() => SessionDatePolicy.Check(6, new DateOnly(2026, 11, 7), TermStart, TermEnd, today: Monday.AddDays(7));

    [Fact]
    public void Check_BeforeTermStart_Throws() =>
        Assert.Equal(SessionDatePolicy.OutsideTerm, Code(() => SessionDatePolicy.Check(1, new DateOnly(2026, 8, 31), TermStart, TermEnd, today: Monday)));

    [Fact]
    public void Check_AfterTermEnd_Throws() =>
        Assert.Equal(SessionDatePolicy.OutsideTerm, Code(() => SessionDatePolicy.Check(1, new DateOnly(2027, 2, 1), TermStart, TermEnd, today: new DateOnly(2027, 3, 1))));

    [Fact]
    public void Check_FirstAndLastDayOfTerm_Pass()
    {
        SessionDatePolicy.Check(2, TermStart, TermStart, TermEnd, today: TermEnd); // 1 Sep 2026 is a Tuesday
        SessionDatePolicy.Check(5, TermEnd, TermStart, TermEnd, today: TermEnd); // 29 Jan 2027 is a Friday
    }
}
