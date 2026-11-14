using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Announcements.Rules;
using SmartSis.Api.Features.Users.Enums;

namespace SmartSis.Tests.Features.Announcements;

/// <summary>PB-12: when an announcement shows, and where its notification points.</summary>
public sealed class AnnouncementScheduleTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(AnnouncementScope.All, true)]
    [InlineData(AnnouncementScope.Students, true)]
    [InlineData(AnnouncementScope.Instructors, true)]
    [InlineData(AnnouncementScope.Major, true)]
    [InlineData(AnnouncementScope.Section, false)]
    public void IsAdminScope_OnlyTheFourAdminAudiences(AnnouncementScope scope, bool expected) =>
        Assert.Equal(expected, AnnouncementSchedule.IsAdminScope(scope));

    [Fact]
    public void ResolvePublishAt_FutureTime_IsScheduled() =>
        Assert.Equal(Now.AddDays(2), AnnouncementSchedule.ResolvePublishAt(Now.AddDays(2), null, Now));

    [Fact]
    public void ResolvePublishAt_EmptyOrPast_OnNewPost_IsNow()
    {
        Assert.Equal(Now, AnnouncementSchedule.ResolvePublishAt(null, null, Now));
        Assert.Equal(Now, AnnouncementSchedule.ResolvePublishAt(Now.AddDays(-3), null, Now));
    }

    [Fact]
    public void ResolvePublishAt_EditOfPublishedPost_KeepsOriginalDate() =>
        Assert.Equal(Now.AddDays(-5), AnnouncementSchedule.ResolvePublishAt(null, Now.AddDays(-5), Now));

    [Fact]
    public void ResolvePublishAt_ScheduledPostEditedToNow_PublishesNow() =>
        Assert.Equal(Now, AnnouncementSchedule.ResolvePublishAt(null, Now.AddDays(5), Now));

    [Theory]
    [InlineData(1, null, AnnouncementStatus.Scheduled)]
    [InlineData(0, null, AnnouncementStatus.Live)]
    [InlineData(-1, 1, AnnouncementStatus.Live)]
    [InlineData(-2, 0, AnnouncementStatus.Expired)]
    [InlineData(-2, -1, AnnouncementStatus.Expired)]
    public void StatusAt_FromPublishAndExpiry(int publishHours, int? expiresHours, AnnouncementStatus expected) =>
        Assert.Equal(expected, AnnouncementSchedule.StatusAt(
            Now.AddHours(publishHours), expiresHours is { } e ? Now.AddHours(e) : null, Now));

    [Fact]
    public void CheckExpiry_None_IsFine() => Assert.Null(AnnouncementSchedule.CheckExpiry(Now, null, Now));

    [Fact]
    public void CheckExpiry_InThePast_IsRejected() =>
        Assert.Equal("The expiry must be in the future.", AnnouncementSchedule.CheckExpiry(Now.AddDays(-2), Now.AddDays(-1), Now));

    [Fact]
    public void CheckExpiry_BeforeScheduledPublish_IsRejected() =>
        Assert.Equal("The expiry must be after the publish time.", AnnouncementSchedule.CheckExpiry(Now.AddDays(3), Now.AddDays(2), Now));

    [Theory]
    [InlineData(UserRole.Student, "/student/announcements?announcement=7")]
    [InlineData(UserRole.Instructor, "/instructor/announcements?announcement=7")]
    [InlineData(UserRole.Admin, "/admin/announcements?announcement=7")]
    public void LinkFor_RoleAnnouncementsPage(UserRole role, string expected)
    {
        Assert.Equal(expected, AnnouncementSchedule.LinkFor(role, 7));
        Assert.EndsWith(AnnouncementSchedule.LinkMarker(7), expected);
    }
}
