using SmartSis.Api.Features.Terms.Models.Dtos;
using SmartSis.Api.Features.Terms.Models.Entities;

namespace SmartSis.Tests.Features.Terms;

[Trait("SR", "SR-21")]
public sealed class TermDtoMappingTests
{
    private static readonly DateTime Base = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Term NewTerm() => new()
    {
        Id = 7, Name = "Fall 2026",
        StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2026, 12, 20),
        EnrollStart = Base, EnrollEnd = Base.AddDays(7),
        AddDropStart = Base.AddDays(7), AddDropEnd = Base.AddDays(14),
        IsCurrent = true,
    };

    [Theory]
    [InlineData(-1, "Closed")]
    [InlineData(0, "Enrollment")]
    [InlineData(7, "AddDrop")]
    [InlineData(14, "Closed")]
    public void ToDto_CurrentWindow_IsWindowAtGivenTime(int dayOffset, string expected) =>
        Assert.Equal(expected, NewTerm().ToDto(Base.AddDays(dayOffset)).CurrentWindow);

    [Fact]
    public void ToDto_CopiesAllFields()
    {
        var dto = NewTerm().ToDto(Base);
        Assert.Equal(new TermDto(7, "Fall 2026", new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 20),
            Base, Base.AddDays(7), Base.AddDays(7), Base.AddDays(14), true, "Enrollment"), dto);
    }
}
