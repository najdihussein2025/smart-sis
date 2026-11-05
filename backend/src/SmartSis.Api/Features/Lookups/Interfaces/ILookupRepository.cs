using SmartSis.Api.Features.Lookups.Models.Dtos;

namespace SmartSis.Api.Features.Lookups.Interfaces;

/// <summary>Projections of the small lookup tables. Not tracked.</summary>
public interface ILookupRepository
{
    Task<List<CampusOptionDto>> CampusesAsync(CancellationToken ct);
    Task<List<DepartmentOptionDto>> DepartmentsAsync(CancellationToken ct);
    Task<List<MajorOptionDto>> MajorsAsync(CancellationToken ct);
    Task<List<RoomOptionDto>> RoomsAsync(CancellationToken ct);

    /// <summary>Instructors whose account is active (deactivated ones can't be assigned to sections).</summary>
    Task<List<InstructorOptionDto>> ActiveInstructorsAsync(CancellationToken ct);
}
