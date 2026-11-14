using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// Chat demo data (SR-23, simulated): one Ready knowledge-base document so the assistant can answer policy questions.
/// Chunks are stored with an empty embedding; the AI service embeds such chunks itself when it searches
/// (docs/decisions.md, PB-15). The document has no stored file, so it can't be re-ingested, only deleted.
/// </summary>
public sealed class ChatDevSeed : IDevSeedStep
{
    public int Order => 700;

    private static readonly string[] Passages =
    [
        "Academic policies (simulated demo text). Enrollment period: students enroll in sections from their cart and may swap a section, "
        + "but cannot drop during the enrollment period. Add/Drop period: students may add, drop and swap sections. Outside both windows "
        + "registration is locked. The exact dates of each window are set per term by the registrar.",
        "Credit load: the default maximum is 18 credits per term. A student's major may set a different cap, and the registrar can grant an "
        + "individual override. Requests for an overload go through a support request in the Registration category.",
        "Prerequisites: a prerequisite counts as passed with a final grade of D or better. A course can be retaken; the latest attempt replaces "
        + "earlier attempts in the GPA.",
        "Attendance: the absence limit of a course is three absences per credit (a 3-credit course allows 9 absences). One absence before the limit "
        + "the student receives a warning, at the limit a final notice, and one absence over the limit the student is dropped from the course "
        + "with a grade of F that counts in the GPA.",
        "Grades and GPA: final grades use a 4.0 scale and the GPA is weighted by credits. Grade disputes must be raised through a support request "
        + "in the Grade dispute category within two weeks of the grade being published.",
        "Payments: tuition charges are issued per major each term with a due date. Partial payments are accepted; a charge is Pending until a "
        + "payment is made, Partial while a balance remains and Paid when settled. Questions about charges go through a Finance support request.",
    ];

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        if (await db.Set<KnowledgeDocument>().AnyAsync(ct)) return;
        var adminId = await db.Set<User>().Where(u => u.Role == UserRole.Admin).OrderBy(u => u.Id).Select(u => u.Id).FirstOrDefaultAsync(ct);
        if (adminId == 0) return;

        var doc = new KnowledgeDocument
        {
            Title = "Academic policies (demo)",
            FileUrl = "",
            UploadedBy = adminId,
            Status = KnowledgeStatus.Ready,
        };
        foreach (var text in Passages)
            doc.Chunks.Add(new KnowledgeChunk { Content = text, Embedding = [] });
        db.Add(doc);
    }
}
