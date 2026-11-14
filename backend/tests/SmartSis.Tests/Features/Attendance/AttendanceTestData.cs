using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Attendance.Enums;
using SmartSis.Api.Features.Attendance.Models.Dtos;
using SmartSis.Api.Features.Attendance.Models.Entities;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Sections.Enums;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Tests.Infrastructure;

namespace SmartSis.Tests.Features.Attendance;

/// <summary>Attendance helpers over the seeded simulated data (SR-23). The seeded term started 30 days ago.</summary>
public static class AttendanceTestData
{
    private static int _fakeWeek;

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>The latest date before today on <paramref name="dayOfWeek"/> (1 = Monday), <paramref name="weeksAgo"/> weeks further back.</summary>
    public static DateOnly PastDate(int dayOfWeek, int weeksAgo = 0)
    {
        var d = Today.AddDays(-1);
        while ((int)d.DayOfWeek != dayOfWeek) d = d.AddDays(-1);
        return d.AddDays(-7 * weeksAgo);
    }

    /// <summary>Removes every session, record and alert, so each test starts from no attendance.</summary>
    public static Task ClearAttendanceAsync(this SisApiFactory f) =>
        f.WithDbAsync(async db =>
        {
            db.RemoveRange(await db.Set<AbsenceAlert>().ToListAsync());
            db.RemoveRange(await db.Set<AttendanceRecord>().ToListAsync());
            db.RemoveRange(await db.Set<AttendanceSession>().ToListAsync());
            await db.SaveChangesAsync();
        });

    /// <summary>The first meeting (lowest day) of a seeded current-term section.</summary>
    public static async Task<(int MeetingId, int DayOfWeek)> MeetingAsync(this SisApiFactory f, string courseCode, string sectionCode = "A")
    {
        var sectionId = await f.SectionIdAsync(courseCode, sectionCode);
        (int, int) result = default;
        await f.WithDbAsync(async db =>
        {
            var m = await db.Set<SectionMeeting>().Where(x => x.SectionId == sectionId).OrderBy(x => x.DayOfWeek).FirstAsync();
            result = (m.Id, m.DayOfWeek);
        });
        return result;
    }

    /// <summary>Puts a simulated student in a section (Enrolled). Returns the enrollment id.</summary>
    public static async Task<int> EnrollDirectlyAsync(this SisApiFactory f, long universityId, int sectionId)
    {
        await f.AddStudentAsync(universityId);
        var userId = await f.UserIdAsync(universityId);
        var id = 0;
        await f.WithDbAsync(async db =>
        {
            var e = new Enrollment { StudentId = userId, SectionId = sectionId, EnrolledAt = DateTime.UtcNow.AddDays(-20) };
            db.Add(e);
            await db.SaveChangesAsync();
            id = e.Id;
        });
        return id;
    }

    /// <summary>Gives the enrollment <paramref name="count"/> earlier absences, each in its own old session of the meeting.</summary>
    public static Task AddPastAbsencesAsync(this SisApiFactory f, int enrollmentId, int meetingId, int count) =>
        f.WithDbAsync(async db =>
        {
            var e = await db.Set<Enrollment>().SingleAsync(x => x.Id == enrollmentId);
            var instructorId = await db.Set<User>().Where(u => u.UniversityId == SisApiFactory.InstructorId).Select(u => u.Id).SingleAsync();
            for (var i = 0; i < count; i++)
            {
                var s = new AttendanceSession
                {
                    SectionId = e.SectionId, MeetingId = meetingId, SessionDate = new DateOnly(2001, 1, 1).AddDays(7 * Interlocked.Increment(ref _fakeWeek)),
                    SubmittedBy = instructorId, SubmittedAt = DateTime.UtcNow,
                };
                s.Records.Add(new AttendanceRecord { EnrollmentId = enrollmentId, Status = AttendanceStatus.Absent });
                db.Add(s);
            }
            await db.SaveChangesAsync();
        });

    /// <summary>A section in the current term taught by another (simulated) instructor, with one Monday meeting.</summary>
    public static async Task<(int SectionId, int MeetingId)> OtherInstructorsSectionAsync(this SisApiFactory f)
    {
        (int, int) result = default;
        await f.WithDbAsync(async db =>
        {
            var existing = await db.Set<Section>().Include(s => s.Meetings).FirstOrDefaultAsync(s => s.SectionCode == "Z9");
            if (existing is not null)
            {
                result = (existing.Id, existing.Meetings.First().Id);
                return;
            }
            var template = await db.Set<Section>().Include(s => s.Meetings).Include(s => s.Instructor)
                .FirstAsync(s => s.Course.Code == "CSC 310" && s.Term.IsCurrent);
            var user = new User
            {
                UniversityId = 2902, Email = "instructor2902@smartsis.local", FullName = "Other Instructor", Role = UserRole.Instructor,
                PasswordHash = "x",
            };
            var instructor = new Instructor { User = user, DepartmentId = template.Instructor!.DepartmentId };
            var section = new Section { CourseId = template.CourseId, TermId = template.TermId, SectionCode = "Z9", Instructor = instructor, Capacity = 10 };
            section.Meetings.Add(new SectionMeeting
            {
                DayOfWeek = 1, StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 15), RoomId = template.Meetings.First().RoomId,
                Type = MeetingType.Lecture,
            });
            db.AddRange(user, instructor, section);
            await db.SaveChangesAsync();
            result = (section.Id, section.Meetings.First().Id);
        });
        return result;
    }

    public static Task<HttpResponseMessage> GetSheetAsync(this HttpClient client, int meetingId, DateOnly date) =>
        client.GetAsync($"/api/attendance/sheet?meetingId={meetingId}&date={date:yyyy-MM-dd}");

    /// <summary>Loads the roster and submits it: the given enrollments Absent, everyone else Present.</summary>
    public static async Task<HttpResponseMessage> SubmitAsync(this HttpClient client, int meetingId, DateOnly date, params int[] absent)
    {
        var sheetRes = await client.GetSheetAsync(meetingId, date);
        sheetRes.EnsureSuccessStatusCode();
        var sheet = await sheetRes.ReadAsync<AttendanceSheetDto>();
        var marks = sheet.Students
            .Select(s => new AttendanceMarkRequest(s.EnrollmentId, absent.Contains(s.EnrollmentId) ? AttendanceStatus.Absent : AttendanceStatus.Present))
            .ToList();
        return await client.PostAsJsonAsync("/api/attendance/sessions", new SubmitAttendanceRequest(meetingId, date, marks), TestData.Json);
    }
}
