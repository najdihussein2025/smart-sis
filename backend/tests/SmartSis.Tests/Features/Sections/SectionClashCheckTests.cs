using SmartSis.Api.Features.Sections.Rules;

namespace SmartSis.Tests.Features.Sections;

/// <summary>UC-16 / SD-16: room and instructor clashes, plus overlaps inside one section.</summary>
[Trait("SR", "SR-21")]
public sealed class SectionClashCheckTests
{
    private const int RoomA = 1, RoomB = 2, Teacher = 50, OtherTeacher = 51;

    private static MeetingSlot Slot(int day, int h1, int m1, int h2, int m2, int room = RoomA) =>
        new(day, new TimeOnly(h1, m1), new TimeOnly(h2, m2), room);

    private static ScheduledMeeting Other(int day, int h1, int m1, int h2, int m2, int room, int? instructor, string label = "CSC 101 A") =>
        new(label, instructor, day, new TimeOnly(h1, m1), new TimeOnly(h2, m2), room, room == RoomA ? "B-101" : "B-204");

    [Fact]
    public void Find_NothingElseScheduled_NoClash() =>
        Assert.Null(SectionClashCheck.Find([Slot(1, 8, 0, 9, 15), Slot(3, 8, 0, 9, 15)], Teacher, []));

    [Fact]
    public void Find_OwnMeetingsOverlap_SameSection()
    {
        var clash = SectionClashCheck.Find([Slot(2, 8, 0, 9, 15), Slot(2, 9, 0, 10, 0, RoomB)], null, []);

        Assert.Equal(new Clash(ClashKind.SameSection, 0, OtherIndex: 1), clash);
    }

    [Fact]
    public void Find_OwnMeetingsBackToBack_NoClash() =>
        Assert.Null(SectionClashCheck.Find([Slot(2, 8, 0, 9, 15), Slot(2, 9, 15, 10, 30)], null, []));

    [Fact]
    public void Find_SameRoomOverlap_RoomClash()
    {
        var other = Other(1, 8, 0, 9, 15, RoomA, OtherTeacher);

        var clash = SectionClashCheck.Find([Slot(1, 9, 0, 10, 0)], Teacher, [other]);

        Assert.Equal(new Clash(ClashKind.Room, 0, Other: other), clash);
    }

    [Fact]
    public void Find_SameRoomBackToBack_NoClash() =>
        Assert.Null(SectionClashCheck.Find([Slot(1, 9, 15, 10, 30)], Teacher, [Other(1, 8, 0, 9, 15, RoomA, OtherTeacher)]));

    [Fact]
    public void Find_SameRoomOtherDay_NoClash() =>
        Assert.Null(SectionClashCheck.Find([Slot(2, 8, 0, 9, 15)], Teacher, [Other(1, 8, 0, 9, 15, RoomA, OtherTeacher)]));

    [Fact]
    public void Find_SameInstructorOtherRoom_InstructorClash()
    {
        var other = Other(4, 10, 0, 12, 0, RoomB, Teacher);

        var clash = SectionClashCheck.Find([Slot(4, 11, 0, 12, 15, RoomA)], Teacher, [other]);

        Assert.Equal(new Clash(ClashKind.Instructor, 0, Other: other), clash);
    }

    [Fact]
    public void Find_NoInstructor_IgnoresOtherSectionsTeachers() =>
        Assert.Null(SectionClashCheck.Find([Slot(4, 11, 0, 12, 15, RoomA)], null, [Other(4, 10, 0, 12, 0, RoomB, Teacher)]));

    [Fact]
    public void Find_OtherSectionHasNoInstructor_NoInstructorClash() =>
        Assert.Null(SectionClashCheck.Find([Slot(4, 11, 0, 12, 15, RoomA)], Teacher, [Other(4, 10, 0, 12, 0, RoomB, null)]));

    [Fact]
    public void Find_RoomAndInstructorBothClash_ReportsRoomFirst()
    {
        var other = Other(5, 9, 0, 11, 0, RoomA, Teacher);

        var clash = SectionClashCheck.Find([Slot(5, 10, 0, 11, 0, RoomA)], Teacher, [other]);

        Assert.Equal(ClashKind.Room, clash!.Kind);
    }

    [Fact]
    public void Find_SecondMeetingClashes_ReportsItsIndex()
    {
        var other = Other(3, 8, 0, 9, 15, RoomA, OtherTeacher);

        var clash = SectionClashCheck.Find([Slot(1, 8, 0, 9, 15), Slot(3, 8, 30, 9, 45)], Teacher, [other]);

        Assert.Equal(1, clash!.MeetingIndex);
    }

    [Theory]
    [InlineData(8, 0, 9, 15, true)]   // identical
    [InlineData(7, 0, 8, 1, true)]    // overlaps the start by a minute
    [InlineData(9, 14, 10, 0, true)]  // overlaps the end by a minute
    [InlineData(8, 15, 9, 0, true)]   // inside
    [InlineData(7, 0, 10, 0, true)]   // contains
    [InlineData(7, 0, 8, 0, false)]   // ends when the other starts
    [InlineData(9, 15, 10, 0, false)] // starts when the other ends
    public void Overlaps_Boundaries(int h1, int m1, int h2, int m2, bool expected) =>
        Assert.Equal(expected, SectionClashCheck.Overlaps(Slot(1, h1, m1, h2, m2), 1, new TimeOnly(8, 0), new TimeOnly(9, 15)));
}
