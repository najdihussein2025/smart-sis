using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Finance.Enums;
using SmartSis.Api.Features.Finance.Interfaces;
using SmartSis.Api.Features.Finance.Models.Dtos;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Repositories;

public sealed class FinanceRepository(SisDbContext db) : IFinanceRepository
{
    private IQueryable<Student> ActiveStudents => db.Set<Student>().Where(s => s.User.IsActive && s.User.DeletedAt == null);

    public async Task<FinanceOptionsDto> GetOptionsAsync(CancellationToken ct)
    {
        var terms = await db.Set<Term>().AsNoTracking()
            .OrderByDescending(t => t.StartDate).ThenByDescending(t => t.Id)
            .Select(t => new FinanceTermOptionDto(t.Id, t.Name, t.IsCurrent)).ToListAsync(ct);
        var active = ActiveStudents;
        var majors = await db.Set<Major>().AsNoTracking().OrderBy(m => m.Name)
            .Select(m => new FinanceMajorOptionDto(m.Id, m.Name, active.Count(s => s.MajorId == m.Id))).ToListAsync(ct);
        var types = await db.Set<PaymentType>().AsNoTracking().OrderBy(p => p.Name)
            .Select(p => new PaymentTypeOptionDto(p.Id, p.Name)).ToListAsync(ct);
        return new FinanceOptionsDto(terms, majors, types);
    }

    public Task<string?> MajorNameAsync(int majorId, CancellationToken ct) =>
        db.Set<Major>().Where(m => m.Id == majorId).Select(m => m.Name).FirstOrDefaultAsync(ct);

    public Task<bool> TermExistsAsync(int termId, CancellationToken ct) => db.Set<Term>().AnyAsync(t => t.Id == termId, ct);

    public Task<bool> PaymentTypeExistsAsync(int paymentTypeId, CancellationToken ct) =>
        db.Set<PaymentType>().AnyAsync(p => p.Id == paymentTypeId, ct);

    public Task<List<ChargeRecipient>> ActiveStudentsOfMajorAsync(int majorId, CancellationToken ct) =>
        ActiveStudents.Where(s => s.MajorId == majorId).OrderBy(s => s.User.UniversityId)
            .Select(s => new ChargeRecipient(s.UserId, s.User.UniversityId, s.User.FullName, s.User.Email)).ToListAsync(ct);

    public Task<ChargeRecipient?> ActiveStudentByUniversityIdAsync(long universityId, CancellationToken ct) =>
        ActiveStudents.Where(s => s.User.UniversityId == universityId)
            .Select(s => new ChargeRecipient(s.UserId, s.User.UniversityId, s.User.FullName, s.User.Email)).FirstOrDefaultAsync(ct);

    public Task<ChargeRecipient?> ActiveStudentAsync(int userId, CancellationToken ct) =>
        ActiveStudents.Where(s => s.UserId == userId)
            .Select(s => new ChargeRecipient(s.UserId, s.User.UniversityId, s.User.FullName, s.User.Email)).FirstOrDefaultAsync(ct);

    public async Task<PagedResult<ChargeBatchDto>> SearchBatchesAsync(int? termId, int? majorId, PageQuery page, CancellationToken ct)
    {
        var q = db.Set<ChargeBatch>().AsNoTracking();
        if (termId is not null) q = q.Where(b => b.TermId == termId);
        if (majorId is not null) q = q.Where(b => b.MajorId == majorId);

        var total = await q.CountAsync(ct);
        var items = await ProjectBatches(q.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id).Skip(page.Skip).Take(page.Take))
            .ToListAsync(ct);
        return new PagedResult<ChargeBatchDto>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public Task<ChargeBatchDto?> GetBatchAsync(int id, CancellationToken ct) =>
        ProjectBatches(db.Set<ChargeBatch>().AsNoTracking().Where(b => b.Id == id)).FirstOrDefaultAsync(ct);

    private IQueryable<ChargeBatchDto> ProjectBatches(IQueryable<ChargeBatch> q)
    {
        var charges = db.Set<Charge>();
        return q.Select(b => new ChargeBatchDto(
            b.Id, b.Title, b.MajorId, b.Major.Name, b.TermId, b.Term.Name, b.PaymentTypeId, b.PaymentType.Name,
            b.Amount, b.DueDate,
            charges.Count(c => c.BatchId == b.Id),
            charges.Count(c => c.BatchId == b.Id && c.Status == ChargeStatus.Paid),
            b.CreatedAt));
    }

    public async Task<PagedResult<ChargeRow>> SearchChargesAsync(ChargeSearchQuery query, PageQuery page, CancellationToken ct)
    {
        // Deactivated students' charges stay visible to the admin (payment history is kept).
        var q = db.Set<Charge>().IgnoreQueryFilters().AsNoTracking();
        if (query.Status is not null) q = q.Where(c => c.Status == query.Status);
        if (query.TermId is not null) q = q.Where(c => c.TermId == query.TermId);
        if (query.MajorId is not null) q = q.Where(c => c.Student.MajorId == query.MajorId);
        if (query.BatchId is not null) q = q.Where(c => c.BatchId == query.BatchId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var text = query.Search.Trim();
            if (long.TryParse(text, out var universityId))
                q = q.Where(c => c.Student.User.UniversityId == universityId);
            else
            {
                // ToLower: SQL Server's collation is case-insensitive anyway, EF InMemory (tests) is not.
                var lower = text.ToLower();
                q = q.Where(c => c.Student.User.FullName.ToLower().Contains(lower));
            }
        }

        var total = await q.CountAsync(ct);
        // Unpaid first, then by due date.
        var items = await ProjectCharges(q
                .OrderBy(c => c.Status == ChargeStatus.Paid).ThenBy(c => c.DueDate)
                .ThenBy(c => c.Student.User.UniversityId).ThenBy(c => c.Id)
                .Skip(page.Skip).Take(page.Take))
            .ToListAsync(ct);
        return new PagedResult<ChargeRow>(items, Math.Max(page.Page, 1), page.Take, total);
    }

    public Task<ChargeRow?> GetChargeAsync(int id, CancellationToken ct) =>
        ProjectCharges(db.Set<Charge>().IgnoreQueryFilters().AsNoTracking().Where(c => c.Id == id)).FirstOrDefaultAsync(ct);

    public Task<List<ChargeRow>> ListForStudentAsync(int studentId, CancellationToken ct) =>
        ProjectCharges(db.Set<Charge>().AsNoTracking().Where(c => c.StudentId == studentId)
                .OrderBy(c => c.DueDate).ThenBy(c => c.Id))
            .ToListAsync(ct);

    private static IQueryable<ChargeRow> ProjectCharges(IQueryable<Charge> q) =>
        q.Select(c => new ChargeRow(
            c.Id, c.StudentId, c.Student.User.UniversityId, c.Student.User.FullName, c.Student.Major.Name, c.BatchId,
            c.TermId, c.Term.Name, c.PaymentType.Name, c.Title,
            c.Amount, c.Payments.Sum(p => (decimal?)p.Amount) ?? 0, c.DueDate, c.Status,
            c.Payments.OrderBy(p => p.PaidAt).ThenBy(p => p.Id).Select(p => new PaymentDto(p.Id, p.Amount, p.PaidAt, p.Note)).ToList()));

    public async Task<Charge?> LockChargeAsync(int id, CancellationToken ct)
    {
        if (db.Database.IsRelational())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT Id FROM Charges WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}", ct);
        return await db.Set<Charge>().Include(c => c.Payments).FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public void Add(ChargeBatch batch) => db.Set<ChargeBatch>().Add(batch);
    public void Add(Charge charge) => db.Set<Charge>().Add(charge);
}
