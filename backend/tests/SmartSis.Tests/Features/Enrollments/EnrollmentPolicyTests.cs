using SmartSis.Api.Features.Enrollments.Rules;
using SmartSis.Api.Features.Terms.Enums;
using SmartSis.Api.Shared.Exceptions;

namespace SmartSis.Tests.Features.Enrollments;

/// <summary>Rules E1–E6, in the check order window → cap → prerequisites → same course → conflict → capacity.</summary>
public sealed class EnrollmentPolicyTests
{
    private static MeetingTime Mon(int h, int m, int minutes) => new(1, new TimeOnly(h, m), new TimeOnly(h, m).AddMinutes(minutes));

    private static HeldSection Held(int sectionId, int courseId, string code, int credits = 3, params MeetingTime[] meetings) =>
        new(sectionId, courseId, code, $"{code} A", credits, meetings);

    private static SectionCandidate Candidate(
        int credits = 3, int capacity = 30, int taken = 0, MeetingTime[]? meetings = null, Prerequisite[]? prereqs = null,
        int sectionId = 100, int courseId = 10, string code = "CSC 310") =>
        new(sectionId, courseId, code, $"{code} A", credits, meetings ?? [Mon(13, 0, 75)], prereqs ?? [], capacity, taken);

    private static StudentEnrollmentState State(
        TermWindow window = TermWindow.Enrollment, int max = 18, HeldSection[]? enrolled = null, int[]? passed = null) =>
        new(window, max, enrolled ?? [], new HashSet<int>(passed ?? []));

    private static string Code(Action act) => Assert.Throws<BusinessRuleException>(act).Code!;

    [Theory]
    [InlineData(TermWindow.Enrollment)]
    [InlineData(TermWindow.AddDrop)]
    [Trait("Rule", "E1")]
    public void CheckEnroll_WindowOpen_Passes(TermWindow window) => EnrollmentPolicy.CheckEnroll(State(window), Candidate());

    [Fact]
    [Trait("Rule", "E1")]
    public void CheckEnroll_WindowClosed_Throws() =>
        Assert.Equal(EnrollmentPolicy.WindowClosed, Code(() => EnrollmentPolicy.CheckEnroll(State(TermWindow.Closed), Candidate())));

    [Fact]
    [Trait("Rule", "E2")]
    public void CheckEnroll_ExactlyAtCap_Passes() =>
        EnrollmentPolicy.CheckEnroll(State(enrolled: [Held(1, 1, "CSC 101", 15)]), Candidate(credits: 3));

    [Fact]
    [Trait("Rule", "E2")]
    public void CheckEnroll_OneOverCap_Throws()
    {
        var ex = Assert.Throws<BusinessRuleException>(() =>
            EnrollmentPolicy.CheckEnroll(State(enrolled: [Held(1, 1, "CSC 101", 16)]), Candidate(credits: 3)));
        Assert.Equal(EnrollmentPolicy.CreditCap, ex.Code);
        Assert.Contains("19 credits", ex.Message);
        Assert.Contains("limit is 18", ex.Message);
    }

    [Fact]
    [Trait("Rule", "E2")]
    public void CheckEnroll_OverrideCap_IsUsed() =>
        EnrollmentPolicy.CheckEnroll(State(max: 21, enrolled: [Held(1, 1, "CSC 101", 18)]), Candidate(credits: 3));

    [Fact]
    [Trait("Rule", "E3")]
    [Trait("TC", "TC_55")]
    public void CheckEnroll_PrerequisiteNotPassed_ThrowsNamingIt()
    {
        var ex = Assert.Throws<BusinessRuleException>(() =>
            EnrollmentPolicy.CheckEnroll(State(), Candidate(prereqs: [new(5, "CSC 210")])));
        Assert.Equal(EnrollmentPolicy.PrerequisiteMissing, ex.Code);
        Assert.Contains("CSC 210", ex.Message);
    }

    [Fact]
    [Trait("Rule", "E3")]
    public void CheckEnroll_PrerequisitePassed_Passes() =>
        EnrollmentPolicy.CheckEnroll(State(passed: [5]), Candidate(prereqs: [new(5, "CSC 210")]));

    [Fact]
    [Trait("Rule", "E4")]
    [Trait("TC", "TC_54")]
    public void CheckEnroll_OtherSectionOfSameCourse_Throws() =>
        Assert.Equal(EnrollmentPolicy.SameCourse,
            Code(() => EnrollmentPolicy.CheckEnroll(State(enrolled: [Held(99, 10, "CSC 310")]), Candidate(meetings: [Mon(18, 0, 60)]))));

    [Fact]
    [Trait("Rule", "E5")]
    [Trait("TC", "TC_56")]
    public void CheckEnroll_OverlappingMeeting_ThrowsNamingTheOtherSection()
    {
        var ex = Assert.Throws<BusinessRuleException>(() =>
            EnrollmentPolicy.CheckEnroll(State(enrolled: [Held(1, 1, "CSC 210", 3, Mon(12, 30, 75))]), Candidate(meetings: [Mon(13, 0, 75)])));
        Assert.Equal(EnrollmentPolicy.ScheduleConflict, ex.Code);
        Assert.Equal("Schedule conflict with CSC 210 A on Monday 12:30–13:45.", ex.Message);
    }

    [Fact]
    [Trait("Rule", "E5")]
    public void CheckEnroll_BackToBack_Passes() =>
        EnrollmentPolicy.CheckEnroll(State(enrolled: [Held(1, 1, "CSC 210", 3, Mon(11, 45, 75))]), Candidate(meetings: [Mon(13, 0, 75)]));

    [Fact]
    [Trait("Rule", "E5")]
    public void CheckEnroll_SameTimeOtherDay_Passes() =>
        EnrollmentPolicy.CheckEnroll(
            State(enrolled: [Held(1, 1, "CSC 210", 3, new MeetingTime(2, new TimeOnly(13, 0), new TimeOnly(14, 15)))]),
            Candidate(meetings: [Mon(13, 0, 75)]));

    [Fact]
    [Trait("Rule", "E6")]
    public void CheckEnroll_LastSeat_Passes() => EnrollmentPolicy.CheckEnroll(State(), Candidate(capacity: 30, taken: 29));

    [Fact]
    [Trait("Rule", "E6")]
    public void CheckEnroll_Full_Throws()
    {
        var ex = Assert.Throws<BusinessRuleException>(() => EnrollmentPolicy.CheckEnroll(State(), Candidate(capacity: 30, taken: 30)));
        Assert.Equal(EnrollmentPolicy.SectionFull, ex.Code);
        Assert.Equal("Section CSC 310 A is full.", ex.Message);
    }

    [Fact]
    public void CheckEnroll_SeveralFailures_ReportsTheFirstInOrder()
    {
        // Over the cap, missing a prerequisite, clashing and full: the cap is reported.
        var state = State(enrolled: [Held(1, 1, "CSC 210", 18, Mon(13, 0, 75))]);
        var candidate = Candidate(prereqs: [new(5, "CSC 210")], taken: 30, capacity: 30);
        Assert.Equal(EnrollmentPolicy.CreditCap, Code(() => EnrollmentPolicy.CheckEnroll(state, candidate)));

        // Within the cap: the prerequisite comes before the conflict and the capacity.
        var lighter = State(enrolled: [Held(1, 1, "CSC 210", 3, Mon(13, 0, 75))]);
        Assert.Equal(EnrollmentPolicy.PrerequisiteMissing, Code(() => EnrollmentPolicy.CheckEnroll(lighter, candidate)));

        // Prerequisite passed: the conflict comes before the capacity.
        var passed = lighter with { PassedCourseIds = new HashSet<int> { 5 } };
        Assert.Equal(EnrollmentPolicy.ScheduleConflict, Code(() => EnrollmentPolicy.CheckEnroll(passed, candidate)));
    }

    [Fact]
    [Trait("Rule", "E1")]
    public void CheckDrop_OnlyDuringAddDrop()
    {
        EnrollmentPolicy.CheckDrop(TermWindow.AddDrop);
        Assert.Equal(EnrollmentPolicy.DropWindowClosed, Code(() => EnrollmentPolicy.CheckDrop(TermWindow.Enrollment)));
        Assert.Equal(EnrollmentPolicy.DropWindowClosed, Code(() => EnrollmentPolicy.CheckDrop(TermWindow.Closed)));
    }
}
