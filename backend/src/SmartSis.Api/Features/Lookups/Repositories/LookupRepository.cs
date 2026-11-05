using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Lookups.Interfaces;
using SmartSis.Api.Features.Lookups.Models.Dtos;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Data;

namespace SmartSis.Api.Features.Lookups.Repositories;

public sealed class LookupRepository(SisDbContext db) : ILookupRepository
{
    public Task<List<CampusOptionDto>> CampusesAsync(CancellationToken ct) =>
        db.Set<Campus>().AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CampusOptionDto(c.Id, c.Name))
            .ToListAsync(ct);

    public Task<List<DepartmentOptionDto>> DepartmentsAsync(CancellationToken ct) =>
        db.Set<Department>().AsNoTracking()
            .OrderBy(d => d.Code)
            .Select(d => new DepartmentOptionDto(d.Id, d.Code, d.Name))
            .ToListAsync(ct);

    public Task<List<MajorOptionDto>> MajorsAsync(CancellationToken ct) =>
        db.Set<Major>().AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new MajorOptionDto(m.Id, m.Name, m.DepartmentId, m.DefaultMaxCredits))
            .ToListAsync(ct);

    public Task<List<RoomOptionDto>> RoomsAsync(CancellationToken ct) =>
        db.Set<Room>().AsNoTracking()
            .OrderBy(r => r.Campus.Name).ThenBy(r => r.Name)
            .Select(r => new RoomOptionDto(r.Id, r.Name, r.CampusId, r.Campus.Name, r.Capacity))
            .ToListAsync(ct);

    // Reading the Users feature's tables in a query is fine (backend.md, "Cross-feature calls").
    public Task<List<InstructorOptionDto>> ActiveInstructorsAsync(CancellationToken ct) =>
        db.Set<Instructor>().AsNoTracking()
            .Where(i => i.User.IsActive)
            .OrderBy(i => i.User.FullName)
            .Select(i => new InstructorOptionDto(i.UserId, i.User.FullName, i.User.UniversityId, i.DepartmentId))
            .ToListAsync(ct);
}
