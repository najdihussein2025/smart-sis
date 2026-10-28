using SmartSis.Api.Features.Terms.Enums;
using SmartSis.Api.Features.Terms.Models.Entities;

namespace SmartSis.Tests.Features.Terms;

/// <summary>Rule E1.</summary>
public sealed class TermWindowTests
{
    private static readonly DateTime Base = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Term NewTerm() => new()
    {
        EnrollStart = Base,
        EnrollEnd = Base.AddDays(7),
        AddDropStart = Base.AddDays(7),
        AddDropEnd = Base.AddDays(14),
    };

    [Theory]
    [InlineData(-1, TermWindow.Closed)]
    [InlineData(0, TermWindow.Enrollment)]
    [InlineData(6, TermWindow.Enrollment)]
    [InlineData(7, TermWindow.AddDrop)]
    [InlineData(13, TermWindow.AddDrop)]
    [InlineData(14, TermWindow.Closed)]
    public void WindowAt_ReturnsOpenWindow(int dayOffset, TermWindow expected) =>
        Assert.Equal(expected, NewTerm().WindowAt(Base.AddDays(dayOffset)));
}
