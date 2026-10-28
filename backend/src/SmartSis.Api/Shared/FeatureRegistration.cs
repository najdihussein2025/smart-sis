using SmartSis.Api.Features.Announcements;
using SmartSis.Api.Features.Attendance;
using SmartSis.Api.Features.Auth;
using SmartSis.Api.Features.Cart;
using SmartSis.Api.Features.Chat;
using SmartSis.Api.Features.Courses;
using SmartSis.Api.Features.Dashboard;
using SmartSis.Api.Features.Enrollment;
using SmartSis.Api.Features.Finance;
using SmartSis.Api.Features.Grading;
using SmartSis.Api.Features.Lookups;
using SmartSis.Api.Features.Notifications;
using SmartSis.Api.Features.Schedule;
using SmartSis.Api.Features.Sections;
using SmartSis.Api.Features.Support;
using SmartSis.Api.Features.Terms;
using SmartSis.Api.Features.Users;

namespace SmartSis.Api.Shared;

/// <summary>One line per feature. Each feature wires its own services in its Module.cs.</summary>
public static class FeatureRegistration
{
    public static IServiceCollection AddFeatures(this IServiceCollection services) => services
        .AddAuthFeature()
        .AddUsersFeature()
        .AddLookupsFeature()
        .AddTermsFeature()
        .AddCoursesFeature()
        .AddSectionsFeature()
        .AddScheduleFeature()
        .AddCartFeature()
        .AddEnrollmentFeature()
        .AddGradingFeature()
        .AddAttendanceFeature()
        .AddFinanceFeature()
        .AddAnnouncementsFeature()
        .AddNotificationsFeature()
        .AddSupportFeature()
        .AddDashboardFeature()
        .AddChatFeature();
}
