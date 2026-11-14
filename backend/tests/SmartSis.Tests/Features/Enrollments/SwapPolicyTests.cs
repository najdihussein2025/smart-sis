using SmartSis.Api.Features.Enrollments.Rules;
using SmartSis.Api.Features.Terms.Enums;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Enrollments;

/// <summary>Swap (TC_63–TC_67): the new section must pass every rule as if the old one were already dropped.</summary>
public sealed class SwapPolicyTests
{
    private static MeetingTime Mon(int h) => new(1, new TimeOnly(h, 0), new TimeOnly(h, 0).AddMinutes(75));

    private static readonly HeldSection Csc210A = new(1, 2, "CSC 210", "CSC 210 A", 3, [Mon(10)]);
    private static readonly HeldSection Mat201A = new(5, 4, "MAT 201", "MAT 201 A", 3, [Mon(14)]);

    private static SectionCandidate To(int sectionId, int courseId, string label, int credits = 3, int h = 10, int taken = 0) =>
        new(sectionId, courseId, label[..7], label, credits, [Mon(h)], [], 30, taken);

    private static StudentEnrollmentState State(TermWindow window = TermWindow.Enrollment, int max = 18, params HeldSection[] enrolled) =>
        new(window, max, enrolled, new HashSet<int>());

    private static string Code(Action act) => Assert.Throws<BusinessRuleException>(act).Code!;

    [Fact]
    [Trait("TC", "TC_63")]
    public void Check_OtherSectionOfSameCourse_AtSameTime_Passes() =>
        // Same course and same time as the section being swapped out: neither counts any more.
        SwapPolicy.Check(State(enrolled: [Csc210A, Mat201A]), 1, To(3, 2, "CSC 210 B", h: 10));

    [Fact]
    [Trait("TC", "TC_64")]
    public void Check_DifferentCourse_Passes() => SwapPolicy.Check(State(enrolled: [Csc210A]), 1, To(9, 7, "CSC 310 A"));

    [Fact]
    [Trait("TC", "TC_65")]
    public void Check_ClashWithAnotherHeldSection_Throws() =>
        Assert.Equal(EnrollmentPolicy.ScheduleConflict, Code(() => SwapPolicy.Check(State(enrolled: [Csc210A, Mat201A]), 1, To(9, 7, "CSC 310 A", h: 14))));

    [Fact]
    [Trait("TC", "TC_66")]
    public void Check_FullSection_Throws() =>
        Assert.Equal(EnrollmentPolicy.SectionFull, Code(() => SwapPolicy.Check(State(enrolled: [Csc210A]), 1, To(3, 2, "CSC 210 B", taken: 30))));

    [Fact]
    [Trait("TC", "TC_67")]
    public void Check_WindowClosed_Throws() =>
        Assert.Equal(EnrollmentPolicy.WindowClosed, Code(() => SwapPolicy.Check(State(TermWindow.Closed, enrolled: [Csc210A]), 1, To(3, 2, "CSC 210 B"))));

    [Fact]
    public void Check_AllowedInAddDrop() => SwapPolicy.Check(State(TermWindow.AddDrop, enrolled: [Csc210A]), 1, To(3, 2, "CSC 210 B"));

    [Fact]
    public void Check_SameSection_Throws() =>
        Assert.Equal(SwapPolicy.SameSection, Code(() => SwapPolicy.Check(State(enrolled: [Csc210A]), 1, To(1, 2, "CSC 210 A"))));

    [Fact]
    public void Check_NotEnrolledInFrom_Throws() =>
        Assert.Equal(SwapPolicy.NotEnrolled, Code(() => SwapPolicy.Check(State(enrolled: [Csc210A]), 42, To(3, 2, "CSC 210 B"))));

    [Fact]
    [Trait("Rule", "E2")]
    public void Check_CreditsOfSwappedOutSectionDontCount()
    {
        // 15 + 3 = 18 enrolled; swapping the 3-credit section for a 4-credit one gives 19 > 18.
        var heavy = new HeldSection(7, 8, "CSC 400", "CSC 400 A", 15, [Mon(18)]);
        Assert.Equal(EnrollmentPolicy.CreditCap, Code(() => SwapPolicy.Check(State(enrolled: [Csc210A, heavy]), 1, To(9, 7, "CSC 310 A", credits: 4))));
        SwapPolicy.Check(State(enrolled: [Csc210A, heavy]), 1, To(9, 7, "CSC 310 A", credits: 3));
    }
}
