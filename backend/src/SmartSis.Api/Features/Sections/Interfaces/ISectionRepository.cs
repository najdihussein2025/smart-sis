using SmartSis.Api.Features.Sections.Models.Dtos;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Sections.Rules;
using SmartSis.Api.Shared.Models;
using SmartSis.Api.Shared.Repositories;

namespace SmartSis.Api.Features.Sections.Interfaces;

public interface ISectionRepository : IRepository<Section>
{
    /// <summary>Paged list ordered by course code then section code. Projection, not tracked.</summary>
    Task<PagedResult<SectionDto>> SearchAsync(int? termId, int? courseId, string? search, PageQuery page, CancellationToken ct);

    /// <summary>Detail projection (not tracked), or null.</summary>
    Task<SectionDto?> GetDetailAsync(int id, CancellationToken ct);

    /// <summary>Tracked section with its meetings, for updating.</summary>
    Task<Section?> GetForUpdateAsync(int id, CancellationToken ct);

    /// <summary>Is the (course, term, code) taken? Code already normalized. Optionally ignores one section.</summary>
    Task<bool> CodeExistsAsync(int courseId, int termId, string code, int? excludeId, CancellationToken ct);

    Task<bool> CourseExistsAsync(int courseId, CancellationToken ct);
    Task<bool> TermExistsAsync(int termId, CancellationToken ct);

    /// <summary>True when the user is an instructor whose account is active.</summary>
    Task<bool> ActiveInstructorExistsAsync(int userId, CancellationToken ct);

    Task<HashSet<int>> ExistingRoomIdsAsync(IEnumerable<int> ids, CancellationToken ct);

    /// <summary>Meetings of every other section in the term, for the clash check.</summary>
    Task<List<ScheduledMeeting>> GetTermMeetingsAsync(int termId, int? excludeSectionId, CancellationToken ct);

    /// <summary>Students enrolled or completed in the section (dropped ones don't hold a seat).</summary>
    Task<int> CountSeatsTakenAsync(int sectionId, CancellationToken ct);

    /// <summary>The meetings, among <paramref name="meetingIds"/>, that already have attendance taken.</summary>
    Task<HashSet<int>> MeetingIdsWithAttendanceAsync(IEnumerable<int> meetingIds, CancellationToken ct);

    void RemoveMeeting(SectionMeeting meeting);
}
