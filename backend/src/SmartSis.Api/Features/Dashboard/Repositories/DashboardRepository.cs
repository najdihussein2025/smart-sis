using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Dashboard.Interfaces;
using SmartSis.Api.Features.Dashboard.Models.Dtos;
using SmartSis.Api.Features.Finance.Enums;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Dashboard.Repositories;

public sealed class DashboardRepository(SisDbContext db) : IDashboardRepository
{
    public async Task<List<(int MeetingId, DateOnly Date)>> TakenSessionsAsync(
        IReadOnlyCollection<int> meetingIds, DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (meetingIds.Count == 0) return [];
        var rows = await db.Set<AttendanceSession>().AsNoTracking()
            .Where(s => meetingIds.Contains(s.MeetingId) && s.SessionDate >= from && s.SessionDate <= to)
            .Select(s => new { s.MeetingId, s.SessionDate })
            .ToListAsync(ct);
        return [.. rows.Select(r => (r.MeetingId, r.SessionDate))];
    }

    public async Task<AdminDashboardDto> GetAdminAsync(CancellationToken ct)
    {
        // The soft-delete filter already hides deleted users; IsActive also leaves out deactivated ones (rule D1).
        var users = db.Set<User>().AsNoTracking().Where(u => u.IsActive);
        var activeStudents = await users.CountAsync(u => u.Role == UserRole.Student, ct);
        var activeInstructors = await users.CountAsync(u => u.Role == UserRole.Instructor, ct);

        var termName = await db.Set<Term>().AsNoTracking().Where(t => t.IsCurrent).Select(t => t.Name).FirstOrDefaultAsync(ct);
        var sections = await db.Set<Section>().AsNoTracking().CountAsync(s => s.Term.IsCurrent, ct);

        var requests = db.Set<SupportRequest>().AsNoTracking();
        var open = await requests.CountAsync(r => r.Status == SupportStatus.Open || r.Status == SupportStatus.InProgress, ct);
        var fresh = await requests.CountAsync(r => r.Status == SupportStatus.Open, ct);

        var unpaid = db.Set<Charge>().AsNoTracking().Where(c => c.Status != ChargeStatus.Paid);
        var unpaidCount = await unpaid.CountAsync(ct);
        var charged = await unpaid.SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;
        var paid = await db.Set<Payment>().AsNoTracking()
            .Where(p => p.Charge.Status != ChargeStatus.Paid)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        return new AdminDashboardDto(termName, activeStudents, activeInstructors, sections, open, fresh, unpaidCount, charged - paid);
    }
}
