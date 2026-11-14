using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Announcements.Enums;
using SmartSis.Api.Features.Announcements.Interfaces;
using SmartSis.Api.Features.Announcements.Jobs;
using SmartSis.Api.Features.Announcements.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// PB-12 demo data (simulated, SR-23): live announcements for every audience, one scheduled, one expired, and their
/// in-app notifications (created by the real fan-out, so the bell shows them). Demo Student 3001 has read the oldest one.
/// </summary>
public sealed class AnnouncementsDevSeed : IDevSeedStep
{
    public int Order => 500;

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        if (await db.Set<Announcement>().AnyAsync(ct)) return;

        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var admin = await db.Set<User>().SingleAsync(u => u.UniversityId == 1001, ct);
        var instructor = await db.Set<User>().SingleAsync(u => u.UniversityId == 2001, ct);
        var major = await db.Set<Major>().OrderBy(m => m.Id).FirstAsync(ct);
        var csc210A = await db.Set<Section>().Where(s => s.Term.IsCurrent && s.Course.Code == "CSC 210" && s.SectionCode == "A")
            .FirstOrDefaultAsync(ct);

        Announcement Post(User author, AnnouncementScope scope, string title, string body, double publishedDaysAgo, double? expiresInDays = null) => new()
        {
            Author = author, Scope = scope, Title = title, Body = body,
            PublishAt = now.AddDays(-publishedDaysAgo),
            ExpiresAt = expiresInDays is { } e ? now.AddDays(e) : null,
        };

        var welcome = Post(admin, AnnouncementScope.All, "Welcome to the new Smart SIS",
            "The student information system now runs on the web. Your schedule, cart, grades and attendance are all here.\nReport anything odd through Support.", 5);
        var registration = Post(admin, AnnouncementScope.Students, "Add/drop week is coming",
            "Add/drop opens right after the enrollment window closes. Check your cart and your credit cap before then.", 2, 30);
        var majorPost = Post(admin, AnnouncementScope.Major, "Computer Science advising hours",
            "Advisors are available Monday to Thursday, 10:00–12:00, in room B-204. Bring your planned courses.", 1);
        majorPost.Major = major;
        var staff = Post(admin, AnnouncementScope.Instructors, "Attendance must be submitted weekly",
            "Please submit attendance for every meeting by Friday. Students near the absence limit are warned automatically.", 0.5);
        var expired = Post(admin, AnnouncementScope.All, "Library closed for inventory",
            "The library was closed on Saturday for the yearly inventory.", 20, -10);
        var scheduled = Post(admin, AnnouncementScope.All, "Midterm exam schedule",
            "The midterm exam schedule will be published here. Exams run over two weeks.", -3);
        db.AddRange(welcome, registration, majorPost, staff, expired, scheduled);

        if (csc210A is not null)
        {
            var sectionPost = Post(instructor, AnnouncementScope.Section, "CSC 210 A: lab moved this week",
                "This Thursday's lab is in Lab-3 as usual but starts 15 minutes late. Bring your linked-list assignment.", 0.2);
            sectionPost.Section = csc210A;
            db.Add(sectionPost);
        }
        await db.SaveChangesAsync(ct);

        // The real fan-out: one notification per audience member of every live post.
        await services.GetRequiredService<IAnnouncementService>().NotifyDueAsync(AnnouncementPublishJob.Lookback, ct);

        var student = await db.Set<User>().SingleAsync(u => u.UniversityId == 3001, ct);
        var read = await db.Set<Notification>()
            .Where(n => n.UserId == student.Id && n.Type == NotificationType.Announcement && n.Title == welcome.Title)
            .ToListAsync(ct);
        foreach (var n in read) n.ReadAt = now.AddDays(-4);
    }
}
