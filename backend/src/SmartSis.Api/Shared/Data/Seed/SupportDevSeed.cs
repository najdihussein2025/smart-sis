using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// Simulated support requests with short threads (SR-23), one in each status, so the student page and the admin inbox
/// have something to show. Demo Student (3001) gets three; Demo Student 2 (3002) one.
/// </summary>
public sealed class SupportDevSeed : IDevSeedStep
{
    public int Order => 400;

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        var users = await db.Set<User>().Where(u => u.UniversityId == 1001 || u.UniversityId == 3001 || u.UniversityId == 3002)
            .ToDictionaryAsync(u => u.UniversityId, u => u.Id, ct);
        var categories = await db.Set<SupportCategory>().ToDictionaryAsync(c => c.Name, c => c.Id, ct);
        if (users.Count < 3 || !categories.ContainsKey("Registration")) return;

        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var admin = users[1001];
        var student = users[3001];
        var student2 = users[3002];

        SupportRequest Request(int studentId, string category, string subject, string description, SupportStatus status,
            int? assignee, params (bool FromAdmin, string Body)[] thread)
        {
            var r = new SupportRequest
            {
                StudentId = studentId, CategoryId = categories[category], Subject = subject, Description = description,
                Status = status, AssignedAdminId = assignee,
                ResolvedAt = status is SupportStatus.Resolved or SupportStatus.Closed ? now.AddDays(-1) : null,
            };
            foreach (var (fromAdmin, body) in thread)
                r.Messages.Add(new SupportMessage { AuthorId = fromAdmin ? admin : studentId, Body = body });
            return r;
        }

        db.AddRange(
            Request(student, "Registration", "Can't add CSC 310 to my cart",
                "The catalog says CSC 310 needs CSC 210. I'm taking CSC 210 this term; can I register for CSC 310 now?",
                SupportStatus.InProgress, admin,
                (true, "Hello, prerequisites must be passed before you enroll. We're checking whether an exception applies."),
                (false, "Thank you, I'll wait for your answer.")),
            Request(student, "Finance", "Lab fee charged twice",
                "I see two lab fee charges on my financial account for this term.",
                SupportStatus.Resolved, admin,
                (true, "You're right, the duplicate charge was removed. Sorry for the trouble.")),
            Request(student, "Technical", "Profile photo doesn't upload",
                "Uploading a photo on my profile page does nothing.",
                SupportStatus.Open, null),
            Request(student2, "Grade dispute", "CSC 101 final grade",
                "I believe my CSC 101 final exam score was entered incorrectly.",
                SupportStatus.Closed, admin,
                (true, "The instructor checked the exam and the grade is correct. Closing this request.")));
    }
}
