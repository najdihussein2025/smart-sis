using SmartSis.Api.Features.Sections.Models.Entities;

namespace SmartSis.Tests.Features.Sections;

/// <summary>Rule E5 building block.</summary>
public sealed class SectionMeetingTests
{
    private static SectionMeeting M(int day, int h1, int m1, int h2, int m2) =>
        new() { DayOfWeek = day, StartTime = new TimeOnly(h1, m1), EndTime = new TimeOnly(h2, m2) };

    [Fact] public void SameDayOverlap_Conflicts() => Assert.True(M(1, 8, 0, 9, 15).OverlapsWith(M(1, 9, 0, 10, 15)));
    [Fact] public void BackToBack_DoesNotConflict() => Assert.False(M(1, 8, 0, 9, 15).OverlapsWith(M(1, 9, 15, 10, 30)));
    [Fact] public void DifferentDay_DoesNotConflict() => Assert.False(M(1, 8, 0, 9, 15).OverlapsWith(M(2, 8, 0, 9, 15)));
    [Fact] public void Contained_Conflicts() => Assert.True(M(3, 8, 0, 12, 0).OverlapsWith(M(3, 9, 0, 10, 0)));
}
