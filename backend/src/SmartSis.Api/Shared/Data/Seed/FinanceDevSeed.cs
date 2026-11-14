using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// Simulated charges and payments (SR-23) so the financial account and the admin payments screen have data:
/// a current-term tuition batch for the Computer Science major (one paid, one partial, the rest pending),
/// a paid registration fee last term, and an overdue one-off lab fee for Demo Student (3001).
/// </summary>
public sealed class FinanceDevSeed : IDevSeedStep
{
    public int Order => 300;

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var admin = await db.Set<User>().FirstOrDefaultAsync(u => u.Role == UserRole.Admin, ct);
        var major = await db.Set<Major>().FirstOrDefaultAsync(m => m.Name == "Computer Science", ct);
        var current = await db.Set<Term>().FirstOrDefaultAsync(t => t.IsCurrent, ct);
        var past = await db.Set<Term>().Where(t => !t.IsCurrent).OrderByDescending(t => t.StartDate).FirstOrDefaultAsync(ct);
        var types = await db.Set<PaymentType>().ToDictionaryAsync(p => p.Name, p => p.Id, ct);
        if (admin is null || major is null || current is null || past is null
            || !types.TryGetValue("Tuition", out var tuition) || !types.TryGetValue("Registration", out var registration)
            || !types.TryGetValue("Lab fee", out var labFee))
            return;

        var students = await db.Set<Student>().Include(s => s.User)
            .Where(s => s.MajorId == major.Id && s.User.IsActive)
            .OrderBy(s => s.User.UniversityId).ToListAsync(ct);
        if (students.Count == 0) return;

        // Current-term tuition for the whole major (rule F1).
        var batch = new ChargeBatch
        {
            Major = major, Term = current, PaymentTypeId = tuition, Title = $"{current.Name} tuition",
            Amount = 4500m, DueDate = today.AddDays(20), CreatedBy = admin.Id,
        };
        db.Add(batch);
        var charges = students.Select(s => new Charge
        {
            StudentId = s.UserId, Batch = batch, Term = current, PaymentTypeId = tuition,
            Title = batch.Title, Amount = batch.Amount, DueDate = batch.DueDate,
        }).ToList();
        db.AddRange(charges);

        // Partial and full payments (rule F2), applied through the entity so the status stays right.
        charges[0].ApplyPayment(2000m, admin.Id, now.AddDays(-5), "Simulated receipt 0001");
        if (charges.Count > 1) charges[1].ApplyPayment(4500m, admin.Id, now.AddDays(-3), "Simulated receipt 0002");
        if (charges.Count > 2) charges[2].ApplyPayment(1000m, admin.Id, now.AddDays(-2), null);

        // Demo Student's history: last term's registration fee (paid) and an overdue one-off lab fee.
        var demo = students[0];
        var registrationFee = new Charge
        {
            StudentId = demo.UserId, Term = past, PaymentTypeId = registration, Title = $"{past.Name} registration fee",
            Amount = 250m, DueDate = past.StartDate,
        };
        registrationFee.ApplyPayment(250m, admin.Id, now.AddDays(-250), "Simulated receipt 0000");
        var lab = new Charge
        {
            StudentId = demo.UserId, Term = current, PaymentTypeId = labFee, Title = "CSC 210 lab fee",
            Amount = 150m, DueDate = today.AddDays(-5),
        };
        db.AddRange(registrationFee, lab);
    }
}
