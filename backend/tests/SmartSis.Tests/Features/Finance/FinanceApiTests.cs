using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Finance.Enums;
using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Finance;

/// <summary>
/// PB-18 / T59: charge batches (F1), payments (F2, F3) and the student's own account. Each test uses its own simulated
/// students (SR-23) so the shared in-memory database can't make tests depend on each other.
/// </summary>
public sealed class FinanceApiTests(SisApiFactory factory) : IClassFixture<SisApiFactory>
{
    private const string Admin = "/api/admin/finance";
    private static long _nextUniversityId = 700_000;

    private static long NextUniversityId() => Interlocked.Increment(ref _nextUniversityId);

    private Task<HttpClient> AdminAsync() => factory.ClientForAsync(SisApiFactory.AdminId);

    private sealed record Refs(int CurrentTermId, int TuitionId, int CsMajorId, int DepartmentId, int CampusId);

    private async Task<Refs> RefsAsync()
    {
        Refs? refs = null;
        await factory.WithDbAsync(async db => refs = new Refs(
            (await db.Set<Term>().SingleAsync(t => t.IsCurrent)).Id,
            (await db.Set<PaymentType>().SingleAsync(p => p.Name == "Tuition")).Id,
            (await db.Set<Major>().SingleAsync(m => m.Name == "Computer Science")).Id,
            (await db.Set<Department>().FirstAsync()).Id,
            (await db.Set<Campus>().FirstAsync()).Id));
        return refs!;
    }

    private async Task<int> NewMajorAsync()
    {
        var refs = await RefsAsync();
        var id = 0;
        await factory.WithDbAsync(async db =>
        {
            var major = new Major { DepartmentId = refs.DepartmentId, Name = $"Test major {Guid.NewGuid():N}" };
            db.Add(major);
            await db.SaveChangesAsync();
            id = major.Id;
        });
        return id;
    }

    /// <summary>A simulated student of the given major (dev password). Returns (userId, universityId).</summary>
    private async Task<(int UserId, long UniversityId)> NewStudentAsync(int? majorId = null, bool active = true)
    {
        var refs = await RefsAsync();
        var universityId = NextUniversityId();
        var userId = 0;
        await factory.WithDbAsync(async db =>
        {
            var hash = (await db.Set<User>().FirstAsync(u => u.UniversityId == SisApiFactory.StudentId)).PasswordHash;
            var user = new User
            {
                UniversityId = universityId, Email = $"student{universityId}@smartsis.local", FullName = $"Finance Student {universityId}",
                Role = UserRole.Student, PasswordHash = hash,
            };
            if (!active) user.MarkDeleted(DateTime.UtcNow);
            db.Add(user);
            db.Add(new Student { User = user, MajorId = majorId ?? refs.CsMajorId, CampusId = refs.CampusId, EnrollmentDate = new DateOnly(2025, 9, 1) });
            await db.SaveChangesAsync();
            userId = user.Id;
        });
        return (userId, universityId);
    }

    private async Task<AdminChargeDto> NewChargeAsync(HttpClient admin, long universityId, decimal amount, DateOnly? due = null, string title = "Lab fee")
    {
        var refs = await RefsAsync();
        var res = await admin.PostAsJsonAsync($"{Admin}/charges",
            new CreateChargeRequest(universityId, refs.CurrentTermId, refs.TuitionId, title, amount, due ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return await res.ReadAsync<AdminChargeDto>();
    }

    private static Task<HttpResponseMessage> PayAsync(HttpClient admin, int chargeId, decimal amount, string? note = null) =>
        admin.PostAsJsonAsync($"{Admin}/charges/{chargeId}/payments", new RecordPaymentRequest(amount, note));

    // ---------- F1: batches ----------

    [Fact]
    [Trait("BR", "F1")]
    public async Task CreateBatch_ChargesOnlyActiveStudentsOfThatMajor()
    {
        var majorId = await NewMajorAsync();
        var a = await NewStudentAsync(majorId);
        var b = await NewStudentAsync(majorId);
        var deactivated = await NewStudentAsync(majorId, active: false);
        var otherMajor = await NewStudentAsync();
        var refs = await RefsAsync();
        var admin = await AdminAsync();

        var res = await admin.PostAsJsonAsync($"{Admin}/batches",
            new CreateChargeBatchRequest(majorId, refs.CurrentTermId, refs.TuitionId, "  Spring tuition ", 3000m, new DateOnly(2030, 1, 15)));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var batch = await res.ReadAsync<ChargeBatchDto>();
        Assert.Equal(2, batch.ChargeCount);
        Assert.Equal(0, batch.PaidCount);
        Assert.Equal("Spring tuition", batch.Title);

        await factory.WithDbAsync(async db =>
        {
            var charged = await db.Set<Charge>().Where(c => c.BatchId == batch.Id).ToListAsync();
            Assert.Equal([a.UserId, b.UserId], charged.Select(c => c.StudentId).Order());
            Assert.All(charged, c =>
            {
                Assert.Equal(ChargeStatus.Pending, c.Status);
                Assert.Equal(3000m, c.Amount);
                Assert.Equal(refs.CurrentTermId, c.TermId);
            });
            Assert.False(await db.Set<Charge>().AnyAsync(c => c.StudentId == deactivated.UserId || c.StudentId == otherMajor.UserId));

            // Each charged student is told: notification + queued email (C2).
            var notified = await db.Set<Notification>().Where(n => n.Type == NotificationType.Payment && n.Title == "New charge: Spring tuition")
                .Select(n => n.UserId).ToListAsync();
            Assert.Equal([a.UserId, b.UserId], notified.Order());
            Assert.Equal(2, await db.Set<SmartSis.Api.Features.Notifications.Models.Entities.EmailOutboxMessage>()
                .CountAsync(e => e.Subject == "New charge on your account: Spring tuition"));
        });
    }

    [Fact]
    [Trait("BR", "F1")]
    public async Task CreateBatch_MajorWithNoActiveStudents_Returns409AndCreatesNothing()
    {
        var majorId = await NewMajorAsync();
        await NewStudentAsync(majorId, active: false);
        var refs = await RefsAsync();
        var admin = await AdminAsync();

        var res = await admin.PostAsJsonAsync($"{Admin}/batches",
            new CreateChargeBatchRequest(majorId, refs.CurrentTermId, refs.TuitionId, "Nobody", 10m, new DateOnly(2030, 1, 15)));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        await factory.WithDbAsync(async db => Assert.False(await db.Set<ChargeBatch>().AnyAsync(x => x.MajorId == majorId)));
    }

    [Fact]
    public async Task CreateBatch_UnknownReferencesAndBadFields_Return400WithFieldErrors()
    {
        var admin = await AdminAsync();
        var res = await admin.PostAsJsonAsync($"{Admin}/batches",
            new CreateChargeBatchRequest(99_999, 99_999, 99_999, "Tuition", 10m, new DateOnly(2030, 1, 1)));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.ReadAsync<ValidationProblemDetails>();
        Assert.Contains("majorId", problem.Errors.Keys);
        Assert.Contains("termId", problem.Errors.Keys);
        Assert.Contains("paymentTypeId", problem.Errors.Keys);

        var bad = await admin.PostAsJsonAsync($"{Admin}/batches",
            new CreateChargeBatchRequest(1, 1, 1, " ", 10.005m, default));
        var badProblem = await bad.ReadAsync<ValidationProblemDetails>();
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Contains("title", badProblem.Errors.Keys);
        Assert.Contains("amount", badProblem.Errors.Keys);
        Assert.Contains("dueDate", badProblem.Errors.Keys);
    }

    // ---------- F2 / F3: payments ----------

    [Fact]
    [Trait("BR", "F2")]
    public async Task RecordPayment_PartialThenRest_GoesPendingPartialPaid()
    {
        var student = await NewStudentAsync();
        var admin = await AdminAsync();
        var charge = await NewChargeAsync(admin, student.UniversityId, 1000m);
        Assert.Equal(ChargeStatus.Pending, charge.Status);

        var first = await PayAsync(admin, charge.Id, 400m, "Receipt 1");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var partial = await first.ReadAsync<AdminChargeDto>();
        Assert.Equal(ChargeStatus.Partial, partial.Status);
        Assert.Equal(400m, partial.Paid);
        Assert.Equal(600m, partial.Balance);

        var paid = await (await PayAsync(admin, charge.Id, 600m)).ReadAsync<AdminChargeDto>();
        Assert.Equal(ChargeStatus.Paid, paid.Status);
        Assert.Equal(0m, paid.Balance);
        Assert.Equal([400m, 600m], paid.Payments.Select(p => p.Amount));
        Assert.Equal("Receipt 1", paid.Payments[0].Note);

        await factory.WithDbAsync(async db =>
        {
            var notes = await db.Set<Notification>().Where(n => n.UserId == student.UserId && n.Title == "Payment recorded").ToListAsync();
            Assert.Equal(2, notes.Count);
            Assert.All(notes, n => Assert.Equal("/student/finance", n.Link));
        });
    }

    [Fact]
    [Trait("BR", "F3")]
    public async Task RecordPayment_MoreThanBalance_Returns409AndChangesNothing()
    {
        var student = await NewStudentAsync();
        var admin = await AdminAsync();
        var charge = await NewChargeAsync(admin, student.UniversityId, 500m);
        await PayAsync(admin, charge.Id, 200m);

        var res = await PayAsync(admin, charge.Id, 300.01m);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        var problem = await res.ReadAsync<ProblemDetails>();
        Assert.Contains("remaining balance", problem.Title);
        var after = await (await admin.GetAsync($"{Admin}/charges/{charge.Id}")).ReadAsync<AdminChargeDto>();
        Assert.Equal(ChargeStatus.Partial, after.Status);
        Assert.Equal(300m, after.Balance);
        Assert.Single(after.Payments);
    }

    [Fact]
    [Trait("BR", "F3")]
    public async Task RecordPayment_OnPaidCharge_Returns409()
    {
        var student = await NewStudentAsync();
        var admin = await AdminAsync();
        var charge = await NewChargeAsync(admin, student.UniversityId, 50m);
        await PayAsync(admin, charge.Id, 50m);

        var res = await PayAsync(admin, charge.Id, 1m);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task RecordPayment_ZeroAmount_Returns400_UnknownCharge_Returns404()
    {
        var admin = await AdminAsync();
        var student = await NewStudentAsync();
        var charge = await NewChargeAsync(admin, student.UniversityId, 50m);

        Assert.Equal(HttpStatusCode.BadRequest, (await PayAsync(admin, charge.Id, 0m)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PayAsync(admin, 999_999, 1m)).StatusCode);
    }

    // ---------- one-off charges and search ----------

    [Fact]
    public async Task CreateCharge_UnknownOrDeactivatedStudent_Returns400OnUniversityId()
    {
        var refs = await RefsAsync();
        var admin = await AdminAsync();
        var gone = await NewStudentAsync(active: false);

        foreach (var universityId in new[] { 999_999_999L, gone.UniversityId, SisApiFactory.InstructorId })
        {
            var res = await admin.PostAsJsonAsync($"{Admin}/charges",
                new CreateChargeRequest(universityId, refs.CurrentTermId, refs.TuitionId, "Fee", 10m, new DateOnly(2030, 1, 1)));
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            Assert.Contains("universityId", (await res.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
        }
    }

    [Fact]
    public async Task SearchCharges_FiltersByStudentAndStatus()
    {
        var student = await NewStudentAsync();
        var admin = await AdminAsync();
        var c1 = await NewChargeAsync(admin, student.UniversityId, 100m, title: "One");
        var c2 = await NewChargeAsync(admin, student.UniversityId, 100m, title: "Two");
        await PayAsync(admin, c2.Id, 100m);

        var all = await (await admin.GetAsync($"{Admin}/charges?search={student.UniversityId}")).ReadAsync<PagedResult<AdminChargeDto>>();
        Assert.Equal(2, all.TotalCount);
        Assert.Equal(c1.Id, all.Items[0].Id); // unpaid first

        var paid = await (await admin.GetAsync($"{Admin}/charges?search={student.UniversityId}&status=Paid")).ReadAsync<PagedResult<AdminChargeDto>>();
        Assert.Equal(c2.Id, Assert.Single(paid.Items).Id);

        var byName = await (await admin.GetAsync($"{Admin}/charges?search=finance student {student.UniversityId}")).ReadAsync<PagedResult<AdminChargeDto>>();
        Assert.Equal(2, byName.TotalCount);
    }

    [Fact]
    public async Task Options_ListsTermsMajorsAndPaymentTypes()
    {
        var admin = await AdminAsync();
        var options = await (await admin.GetAsync($"{Admin}/options")).ReadAsync<FinanceOptionsDto>();
        Assert.Contains(options.Terms, t => t.IsCurrent);
        Assert.Contains(options.Majors, m => m.Name == "Computer Science" && m.ActiveStudents >= 6);
        Assert.Equal(["Lab fee", "Registration", "Tuition"], options.PaymentTypes.Select(p => p.Name));
    }

    // ---------- student's own account ----------

    [Fact]
    [Trait("SR", "SR-22")]
    public async Task My_ReturnsOnlyTheSignedInStudentsCharges_WithTotals()
    {
        var me = await NewStudentAsync();
        var other = await NewStudentAsync();
        var admin = await AdminAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdue = await NewChargeAsync(admin, me.UniversityId, 1000m, today.AddDays(-2), "Overdue tuition");
        var later = await NewChargeAsync(admin, me.UniversityId, 200m, today.AddDays(10), "Lab fee");
        await NewChargeAsync(admin, other.UniversityId, 777m);
        await PayAsync(admin, overdue.Id, 250m);

        var client = await factory.ClientForAsync(me.UniversityId);
        var res = await client.GetAsync("/api/finance/my");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var account = await res.ReadAsync<MyFinanceDto>();
        Assert.Equal([overdue.Id, later.Id], account.Charges.Select(c => c.Id));
        Assert.Equal(1200m, account.TotalCharged);
        Assert.Equal(250m, account.TotalPaid);
        Assert.Equal(950m, account.OutstandingBalance);
        Assert.Equal(750m, account.OverdueBalance);
        Assert.Equal(today.AddDays(10), account.NextDueDate);

        var first = account.Charges[0];
        Assert.True(first.IsOverdue);
        Assert.Equal(ChargeStatus.Partial, first.Status);
        Assert.Equal(750m, first.Balance);
        Assert.Equal(250m, Assert.Single(first.Payments).Amount);
        Assert.False(account.Charges[1].IsOverdue);
    }

    [Fact]
    public async Task My_NoCharges_ReturnsEmptyAccount()
    {
        var me = await NewStudentAsync();
        var client = await factory.ClientForAsync(me.UniversityId);
        var account = await (await client.GetAsync("/api/finance/my")).ReadAsync<MyFinanceDto>();
        Assert.Empty(account.Charges);
        Assert.Equal(0m, account.OutstandingBalance);
        Assert.Null(account.NextDueDate);
    }

    [Fact]
    public async Task DevSeed_GivesDemoStudentChargesAndPayments()
    {
        var client = await factory.ClientForAsync(SisApiFactory.StudentId);
        var account = await (await client.GetAsync("/api/finance/my")).ReadAsync<MyFinanceDto>();
        Assert.Contains(account.Charges, c => c.Status == ChargeStatus.Partial);
        Assert.Contains(account.Charges, c => c.Status == ChargeStatus.Paid);
        Assert.Contains(account.Charges, c => c.IsOverdue);
    }
}
