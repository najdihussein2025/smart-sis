using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Sections.Enums;

namespace SmartSis.Api.Features.Attendance.Interfaces;

/// <summary>Record attendance (PB-10) and view it (PB-11). The user always comes from the token.</summary>
public interface IAttendanceService
{
    /// <summary>Instructor: sections they teach this term, with their weekly meetings.</summary>
    Task<TeachingSectionsDto> GetTeachingSectionsAsync(CancellationToken ct);

    /// <summary>Instructor: the roster for a meeting on a date, or what was recorded if it was already taken.</summary>
    Task<AttendanceSheetDto> GetSheetAsync(int meetingId, DateOnly date, CancellationToken ct);

    /// <summary>Instructor: a submitted session's records.</summary>
    Task<AttendanceSheetDto> GetSessionAsync(int sessionId, CancellationToken ct);

    /// <summary>Instructor: submitted sessions of a section they teach, newest first.</summary>
    Task<IReadOnlyList<SessionSummaryDto>> ListSessionsAsync(int sectionId, CancellationToken ct);

    /// <summary>Instructor: save Present / Absent for every enrolled student, then apply the absence rules (A2, A3).</summary>
    Task<SubmitAttendanceResultDto> SubmitAsync(SubmitAttendanceRequest request, CancellationToken ct);
}

/// <summary>A meeting with what the date and ownership checks need.</summary>
public sealed record MeetingInfo(
    int MeetingId, int SectionId, int? InstructorId, int DayOfWeek, TimeOnly StartTime, MeetingType Type,
    string CourseCode, string SectionCode, int Credits, DateOnly TermStart, DateOnly TermEnd);

/// <summary>An Enrolled student of a section.</summary>
public sealed record RosterStudent(int EnrollmentId, int StudentId, long UniversityId, string FullName, string Email);

/// <summary>A recorded mark with its student.</summary>
public sealed record RecordedMark(int EnrollmentId, long UniversityId, string FullName, AttendanceStatus Status);

public interface IAttendanceRepository
{
    Task<(int Id, string Name)?> GetCurrentTermAsync(CancellationToken ct);
    Task<List<TeachingSectionDto>> GetTeachingSectionsAsync(int instructorId, int termId, CancellationToken ct);
    Task<MeetingInfo?> GetMeetingAsync(int meetingId, CancellationToken ct);

    /// <summary>Null when the section doesn't exist; else its instructor (null when unassigned).</summary>
    Task<(bool Exists, int? InstructorId)> GetSectionInstructorAsync(int sectionId, CancellationToken ct);

    /// <summary>The session taken for a meeting on a date, if any (rule A4).</summary>
    Task<AttendanceSession?> FindSessionAsync(int meetingId, DateOnly date, CancellationToken ct);
    Task<AttendanceSession?> FindSessionAsync(int sessionId, CancellationToken ct);

    /// <summary>Enrolled students of the section, by name.</summary>
    Task<List<RosterStudent>> GetRosterAsync(int sectionId, CancellationToken ct);

    Task<List<RecordedMark>> GetMarksAsync(int sessionId, CancellationToken ct);

    /// <summary>Saved Absent records per enrollment (enrollments with none are left out).</summary>
    Task<Dictionary<int, int>> GetAbsenceCountsAsync(IReadOnlyCollection<int> enrollmentIds, CancellationToken ct);

    /// <summary>Alert levels already sent per enrollment.</summary>
    Task<Dictionary<int, List<AbsenceAlertLevel>>> GetSentAlertsAsync(IReadOnlyCollection<int> enrollmentIds, CancellationToken ct);

    Task<List<SessionSummaryDto>> ListSessionsAsync(int sectionId, CancellationToken ct);

    void Add(AttendanceSession session);
    void Add(AbsenceAlert alert);
}
