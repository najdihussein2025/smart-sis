using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Sections.Enums;

namespace SmartSis.Api.Features.Attendance.Models.Dtos;

/// <summary>The sections an instructor teaches this term, to pick one for attendance.</summary>
/// <param name="TermId">Current term id, or null when no term is current (no sections then).</param>
/// <param name="TermName">Current term name.</param>
/// <param name="Today">The server's date (UTC). Attendance can't be taken after it.</param>
/// <param name="Sections">Taught sections, by course code then section code.</param>
public sealed record TeachingSectionsDto(int? TermId, string? TermName, DateOnly Today, IReadOnlyList<TeachingSectionDto> Sections);

/// <summary>A section the instructor teaches.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseCode">Course code, e.g. "CSC 210".</param>
/// <param name="CourseName">Course name.</param>
/// <param name="SectionCode">Section code, e.g. "A".</param>
/// <param name="Credits">Course credits.</param>
/// <param name="AbsenceLimit">Credits × 3 (rule A1).</param>
/// <param name="TermStart">First day of the term: the earliest date attendance can be taken.</param>
/// <param name="TermEnd">Last day of the term.</param>
/// <param name="EnrolledCount">Students currently enrolled.</param>
/// <param name="Meetings">Weekly meetings (the "session" to take attendance for).</param>
public sealed record TeachingSectionDto(
    int SectionId,
    string CourseCode,
    string CourseName,
    string SectionCode,
    int Credits,
    int AbsenceLimit,
    DateOnly TermStart,
    DateOnly TermEnd,
    int EnrolledCount,
    IReadOnlyList<AttendanceMeetingDto> Meetings);

/// <summary>A weekly meeting of a section.</summary>
/// <param name="Id">Meeting id.</param>
/// <param name="DayOfWeek">1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">Start, "HH:mm:ss".</param>
/// <param name="EndTime">End.</param>
/// <param name="RoomName">Room.</param>
/// <param name="Type">"Lecture" or "Lab".</param>
public sealed record AttendanceMeetingDto(int Id, int DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, string RoomName, MeetingType Type);

/// <summary>
/// The attendance sheet of one meeting on one date. Before submission it is the roster (status null); after, it is
/// what was recorded (read-only).
/// </summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="MeetingId">Meeting id.</param>
/// <param name="DayOfWeek">The meeting's day, 1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">The meeting's start.</param>
/// <param name="Type">"Lecture" or "Lab".</param>
/// <param name="SessionDate">The date.</param>
/// <param name="SessionId">The submitted session's id, or null when attendance hasn't been taken yet.</param>
/// <param name="SubmittedAt">When it was submitted (UTC), or null.</param>
/// <param name="AbsenceLimit">Credits × 3 (rule A1).</param>
/// <param name="Students">One row per student, by name.</param>
public sealed record AttendanceSheetDto(
    int SectionId,
    string CourseCode,
    string SectionCode,
    int MeetingId,
    int DayOfWeek,
    TimeOnly StartTime,
    MeetingType Type,
    DateOnly SessionDate,
    int? SessionId,
    DateTime? SubmittedAt,
    int AbsenceLimit,
    IReadOnlyList<AttendanceSheetRowDto> Students);

/// <summary>A student on the sheet.</summary>
/// <param name="EnrollmentId">Enrollment id: send it back when submitting.</param>
/// <param name="UniversityId">The student's university ID.</param>
/// <param name="FullName">Full name.</param>
/// <param name="Status">Recorded status, or null before submission.</param>
/// <param name="Absences">Absences in this section so far.</param>
public sealed record AttendanceSheetRowDto(int EnrollmentId, long UniversityId, string FullName, AttendanceStatus? Status, int Absences);

/// <summary>Attendance for one meeting on one date. Every enrolled student must be marked.</summary>
/// <param name="MeetingId">The meeting (session) of the section.</param>
/// <param name="SessionDate">The date; its weekday must be the meeting's, not in the future, inside the term.</param>
/// <param name="Records">Present / Absent for each enrolled student.</param>
public sealed record SubmitAttendanceRequest(int MeetingId, DateOnly SessionDate, IReadOnlyList<AttendanceMarkRequest> Records);

/// <param name="EnrollmentId">From the sheet.</param>
/// <param name="Status">"Present" or "Absent".</param>
public sealed record AttendanceMarkRequest(int EnrollmentId, AttendanceStatus Status);

/// <summary>What a submission saved, and the absence alerts / auto-drops it caused.</summary>
/// <param name="SessionId">The new session's id.</param>
/// <param name="Present">Students marked present.</param>
/// <param name="Absent">Students marked absent.</param>
/// <param name="Alerts">Alerts sent by this submission (Warning, Limit, AutoDropped).</param>
public sealed record SubmitAttendanceResultDto(int SessionId, int Present, int Absent, IReadOnlyList<AbsenceAlertDto> Alerts);

/// <param name="EnrollmentId">Enrollment id.</param>
/// <param name="FullName">The student.</param>
/// <param name="Level">"Warning" (limit − 1), "Limit" (at the limit) or "AutoDropped" (limit + 1).</param>
/// <param name="Absences">Absences after this submission.</param>
/// <param name="Limit">The course's absence limit.</param>
public sealed record AbsenceAlertDto(int EnrollmentId, string FullName, AbsenceAlertLevel Level, int Absences, int Limit);

/// <summary>A submitted session of a section, for the history list.</summary>
/// <param name="SessionId">Session id.</param>
/// <param name="MeetingId">Meeting id.</param>
/// <param name="SessionDate">Date.</param>
/// <param name="DayOfWeek">1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">The meeting's start.</param>
/// <param name="Type">"Lecture" or "Lab".</param>
/// <param name="Present">Students marked present.</param>
/// <param name="Absent">Students marked absent.</param>
/// <param name="SubmittedAt">When it was submitted (UTC).</param>
public sealed record SessionSummaryDto(
    int SessionId, int MeetingId, DateOnly SessionDate, int DayOfWeek, TimeOnly StartTime, MeetingType Type, int Present, int Absent, DateTime SubmittedAt);

/// <summary>The signed-in student's attendance this term (PB-11).</summary>
/// <param name="TermId">Current term id, or null when no term is current (no courses then).</param>
/// <param name="TermName">Current term name.</param>
/// <param name="Courses">One row per course taken this term, by course code.</param>
public sealed record MyAttendanceDto(int? TermId, string? TermName, IReadOnlyList<CourseAttendanceDto> Courses);

/// <summary>Absences in one course.</summary>
/// <param name="SectionId">Section id.</param>
/// <param name="CourseCode">Course code.</param>
/// <param name="CourseName">Course name.</param>
/// <param name="SectionCode">Section code.</param>
/// <param name="Credits">Credits.</param>
/// <param name="Absences">Absences so far.</param>
/// <param name="Limit">Absence limit, credits × 3 (rule A1). One more than this drops the course.</param>
/// <param name="Remaining">Absences left before reaching the limit (0 at or over it).</param>
/// <param name="Standing">"Perfect" (green, 0), "Warning" (orange) or "Critical" (red, limit − 1 or more), rule A5.</param>
/// <param name="EnrollmentStatus">"Enrolled", or "DroppedAbsence" when the absences dropped the course.</param>
/// <param name="SessionsRecorded">Sessions the student was marked in (present or absent).</param>
/// <param name="AbsenceDates">Each absence, newest first.</param>
public sealed record CourseAttendanceDto(
    int SectionId,
    string CourseCode,
    string CourseName,
    string SectionCode,
    int Credits,
    int Absences,
    int Limit,
    int Remaining,
    AbsenceStanding Standing,
    EnrollmentStatus EnrollmentStatus,
    int SessionsRecorded,
    IReadOnlyList<AbsenceDateDto> AbsenceDates);

/// <summary>One absence.</summary>
/// <param name="Date">Date of the session.</param>
/// <param name="DayOfWeek">1 = Monday … 6 = Saturday.</param>
/// <param name="StartTime">The meeting's start.</param>
/// <param name="Type">"Lecture" or "Lab".</param>
public sealed record AbsenceDateDto(DateOnly Date, int DayOfWeek, TimeOnly StartTime, MeetingType Type);
