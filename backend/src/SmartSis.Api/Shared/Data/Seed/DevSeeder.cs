using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Sections.Enums;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Support.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// Development only. Applies migrations and inserts SIMULATED data (SR-23) when the database is empty.
/// Every seeded account uses the password <see cref="DevPassword"/>.
/// </summary>
public static class DevSeeder
{
    public const string DevPassword = "Pass@1234";

    public static async Task RunAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var config = sp.GetRequiredService<IConfiguration>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevSeeder));
        var db = sp.GetRequiredService<SisDbContext>();

        if (!config.GetValue("Seed:DevData", false)) return;
        if (!db.Database.IsRelational()) { await SeedAsync(db, sp, ct); return; }

        if (!db.Database.GetMigrations().Any())
        {
            logger.LogWarning("No EF migrations found. Run: dotnet ef migrations add InitialCreate -p src/SmartSis.Api -o Shared/Data/Migrations");
            return;
        }

        await db.Database.MigrateAsync(ct);
        await SeedAsync(db, sp, ct);
    }

    internal static async Task SeedAsync(SisDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (await db.Set<User>().IgnoreQueryFilters().AnyAsync(ct)) return;

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var clock = sp.GetRequiredService<TimeProvider>();
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = hasher.Hash(DevPassword);

        // Lookups
        var campus = new Campus { Name = "Main Campus" };
        var csDept = new Department { Code = "CSC", Name = "Computer Science" };
        var mathDept = new Department { Code = "MAT", Name = "Mathematics" };
        var csMajor = new Major { Department = csDept, Name = "Computer Science", DefaultMaxCredits = 18 };
        var rooms = new[]
        {
            new Room { Campus = campus, Name = "B-101", Capacity = 40 },
            new Room { Campus = campus, Name = "B-204", Capacity = 30 },
            new Room { Campus = campus, Name = "Lab-3", Capacity = 25 },
        };
        db.AddRange(campus, csDept, mathDept, csMajor);
        db.AddRange(rooms);
        db.AddRange(
            new PaymentType { Name = "Tuition" },
            new PaymentType { Name = "Registration" },
            new PaymentType { Name = "Lab fee" });
        db.AddRange(
            new SupportCategory { Name = "Registration" },
            new SupportCategory { Name = "Grade dispute" },
            new SupportCategory { Name = "Finance" },
            new SupportCategory { Name = "Technical" },
            new SupportCategory { Name = "Other" });

        // Users (simulated)
        var admin = new User { UniversityId = 1001, Email = "admin@smartsis.local", FullName = "Demo Admin", Role = UserRole.Admin, PasswordHash = hash };
        var instructorUser = new User { UniversityId = 2001, Email = "instructor@smartsis.local", FullName = "Demo Instructor", Role = UserRole.Instructor, PasswordHash = hash };
        var studentUser = new User { UniversityId = 3001, Email = "student@smartsis.local", FullName = "Demo Student", Role = UserRole.Student, PasswordHash = hash };
        var instructor = new Instructor { User = instructorUser, Department = csDept };
        var student = new Student { User = studentUser, Major = csMajor, Campus = campus, EnrollmentDate = new DateOnly(2024, 9, 1) };
        db.AddRange(admin, instructorUser, studentUser);
        db.AddRange(instructor);
        db.AddRange(student);

        // Current term with both windows around today so enrollment can be tried right away.
        var today = DateOnly.FromDateTime(now);
        var term = new Term
        {
            Name = $"Fall {today.Year}",
            StartDate = today.AddDays(-30),
            EndDate = today.AddDays(90),
            EnrollStart = now.AddDays(-14),
            EnrollEnd = now.AddDays(7),
            AddDropStart = now.AddDays(7),
            AddDropEnd = now.AddDays(21),
            IsCurrent = true,
        };
        db.Add(term);

        // Courses
        var csc101 = new Course { Code = "CSC 101", Name = "Introduction to Programming", Credits = 3, Department = csDept, Description = "Variables, control flow, functions and basic data structures." };
        var csc210 = new Course { Code = "CSC 210", Name = "Data Structures", Credits = 3, Department = csDept, Description = "Lists, stacks, queues, trees and hash tables." };
        var csc310 = new Course { Code = "CSC 310", Name = "Database Systems", Credits = 3, Department = csDept, Description = "Relational model, SQL, normalization and transactions." };
        var mat201 = new Course { Code = "MAT 201", Name = "Discrete Mathematics", Credits = 3, Department = mathDept, Description = "Logic, sets, relations, combinatorics and graphs." };
        db.AddRange(csc101, csc210, csc310, mat201);
        db.AddRange(
            new CoursePrerequisite { Course = csc210, Prerequisite = csc101 },
            new CoursePrerequisite { Course = csc310, Prerequisite = csc210 });
        db.AddRange(
            new MajorCourse { Major = csMajor, Course = csc101, Type = CourseType.Major },
            new MajorCourse { Major = csMajor, Course = csc210, Type = CourseType.Major },
            new MajorCourse { Major = csMajor, Course = csc310, Type = CourseType.Major },
            new MajorCourse { Major = csMajor, Course = mat201, Type = CourseType.Elective });

        // Sections with weekly meetings
        Section NewSection(Course c, string code, int capacity, params (int Day, int StartHour, int Minutes, Room Room, MeetingType Type)[] slots)
        {
            var s = new Section { Course = c, Term = term, SectionCode = code, Instructor = instructor, Capacity = capacity };
            foreach (var slot in slots)
                s.Meetings.Add(new SectionMeeting
                {
                    DayOfWeek = slot.Day,
                    StartTime = new TimeOnly(slot.StartHour, 0),
                    EndTime = new TimeOnly(slot.StartHour, 0).AddMinutes(slot.Minutes),
                    Room = slot.Room,
                    Type = slot.Type,
                });
            return s;
        }

        db.AddRange(
            NewSection(csc101, "A", 40, (1, 8, 75, rooms[0], MeetingType.Lecture), (3, 8, 75, rooms[0], MeetingType.Lecture)),
            NewSection(csc210, "A", 30, (2, 10, 75, rooms[1], MeetingType.Lecture), (4, 10, 120, rooms[2], MeetingType.Lab)),
            NewSection(csc210, "B", 30, (1, 14, 75, rooms[1], MeetingType.Lecture), (3, 14, 75, rooms[1], MeetingType.Lecture)),
            NewSection(csc310, "A", 30, (2, 13, 75, rooms[0], MeetingType.Lecture), (5, 9, 120, rooms[2], MeetingType.Lab)),
            NewSection(mat201, "A", 40, (1, 10, 75, rooms[0], MeetingType.Lecture), (3, 10, 75, rooms[0], MeetingType.Lecture)));

        await db.SaveChangesAsync(ct);
    }
}
