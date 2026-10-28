using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Enrollments;

public sealed class EnrollmentTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    [Trait("Rule", "A3")]
    public void DropForAbsence_RecordsFThatCountsInGpa()
    {
        var e = new Enrollment();
        e.DropForAbsence(Now);

        Assert.Equal(EnrollmentStatus.DroppedAbsence, e.Status);
        Assert.Equal("F", e.LetterGrade);
        Assert.Equal(0m, e.GradePoints);
        Assert.True(e.CountsInGpa);
    }

    [Fact]
    public void Drop_DoesNotCountInGpa()
    {
        var e = new Enrollment();
        e.Drop(Now);
        Assert.Equal(EnrollmentStatus.Dropped, e.Status);
        Assert.False(e.CountsInGpa);
    }

    [Fact]
    public void Drop_Twice_Throws()
    {
        var e = new Enrollment();
        e.Drop(Now);
        Assert.Throws<BusinessRuleException>(() => e.Drop(Now));
    }

    [Theory]
    [Trait("Rule", "E3")]
    [InlineData(1.0, true)]   // D passes
    [InlineData(1.3, true)]
    [InlineData(0.0, false)]  // F
    public void HasPassed_DOrBetter(double points, bool expected)
    {
        var e = new Enrollment();
        e.Complete(60m, "X", (decimal)points);
        Assert.Equal(expected, e.HasPassed);
    }
}
