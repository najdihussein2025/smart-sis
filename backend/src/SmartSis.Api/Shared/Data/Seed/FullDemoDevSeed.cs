using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Cart.Models.Entities;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Career.Rules;
using SmartSis.Api.Features.Chat.Enums;
using SmartSis.Api.Features.Chat.Models.Entities;
using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Enrollments.Models.Entities;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Notifications.Enums;
using SmartSis.Api.Features.Notifications.Models.Entities;
using SmartSis.Api.Features.Sections.Enums;
using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// OPT-IN extra demo data (simulated, SR-23) so all three demo accounts (admin 1001, instructor 2001, student 3001)
/// have something on every screen. Runs only when <c>Seed:FullDemo</c> is true (set in appsettings.Development.json), so the
/// integration tests keep their small, predictable data. Adds: an older term with a completed MAT 101 (multi-term transcript and
/// GPA), a new CSC 230 section, a swap history, a cart, notifications for all three accounts, a finished career assessment
/// for Demo Student, and a chat conversation. Runs after every other step (Order 800).
/// </summary>
public sealed class FullDemoDevSeed : IDevSeedStep
{
    public int Order => 800;

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        var config = services.GetRequiredService<IConfiguration>();
        if (!config.GetValue("Seed:FullDemo", false)) return;
        if (await db.Set<Course>().AnyAsync(c => c.Code == "MAT 101", ct)) return;

        var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var users = await db.Set<User>().Where(u => u.UniversityId == 1001 || u.UniversityId == 2001 || u.UniversityId == 3001 || u.UniversityId == 3002)
            .ToDictionaryAsync(u => u.UniversityId, u => u.Id, ct);
        var admin = users[1001];
        var instructor = users[2001];
        var student = users[3001];
        var student2 = users[3002];

        var current = await db.Set<Term>().SingleAsync(t => t.IsCurrent, ct);
        var csDept = await db.Set<Department>().SingleAsync(d => d.Code == "CSC", ct);
        var mathDept = await db.Set<Department>().SingleAsync(d => d.Code == "MAT", ct);
        var major = await db.Set<Major>().SingleAsync(m => m.Name == "Computer Science", ct);
        var rooms = await db.Set<Room>().ToDictionaryAsync(r => r.Name, ct);
        var courses = await db.Set<Course>().ToDictionaryAsync(c => c.Code, ct);
        var sections = await db.Set<Section>().Where(s => s.TermId == current.Id).Include(s => s.Course).ToListAsync(ct);
        Section Sec(string code, string letter) => sections.Single(s => s.Course.Code == code && s.SectionCode == letter);

        // Older term + MAT 101, so the transcript spans three terms.
        var older = new Term
        {
            Name = $"Fall {today.Year - 1}",
            StartDate = today.AddDays(-430), EndDate = today.AddDays(-310),
            EnrollStart = now.AddDays(-450), EnrollEnd = now.AddDays(-440),
            AddDropStart = now.AddDays(-440), AddDropEnd = now.AddDays(-420),
        };
        var mat101 = new Course { Code = "MAT 101", Name = "Calculus I", Credits = 3, Department = mathDept, Description = "Limits, derivatives and the first applications of integrals." };
        var csc230 = new Course { Code = "CSC 230", Name = "Web Development", Credits = 3, Department = csDept, Description = "HTML, CSS, JavaScript and building a small full-stack web app." };
        db.AddRange(older, mat101, csc230);
        db.AddRange(
            new CoursePrerequisite { Course = csc230, Prerequisite = courses["CSC 101"] },
            new MajorCourse { Major = major, Course = mat101, Type = CourseType.Elective },
            new MajorCourse { Major = major, Course = csc230, Type = CourseType.Major });

        var oldMat101 = new Section { Course = mat101, Term = older, SectionCode = "A", InstructorId = instructor, Capacity = 40 };
        oldMat101.Meetings.Add(new SectionMeeting { DayOfWeek = 1, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 15), Room = rooms["B-101"], Type = MeetingType.Lecture });
        var csc230A = new Section { Course = csc230, Term = current, SectionCode = "A", InstructorId = instructor, Capacity = 30 };
        csc230A.Meetings.Add(new SectionMeeting { DayOfWeek = 3, StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(16, 15), Room = rooms["B-204"], Type = MeetingType.Lecture });
        csc230A.Meetings.Add(new SectionMeeting { DayOfWeek = 5, StartTime = new TimeOnly(15, 0), EndTime = new TimeOnly(17, 0), Room = rooms["Lab-3"], Type = MeetingType.Lab });
        db.AddRange(oldMat101, csc230A);

        // Past results (grade/letter/points pairs already used by the base seed) and a swap: 3001 started in CSC 210 B, moved to A.
        db.AddRange(
            new Enrollment { StudentId = student, Section = oldMat101, Status = EnrollmentStatus.Completed, FinalPct = 91, LetterGrade = "A-", GradePoints = 3.7m, EnrolledAt = now.AddDays(-445) },
            new Enrollment { StudentId = student2, Section = oldMat101, Status = EnrollmentStatus.Completed, FinalPct = 74, LetterGrade = "C+", GradePoints = 2.3m, EnrolledAt = now.AddDays(-445) },
            new Enrollment
            {
                StudentId = student, Section = Sec("CSC 210", "B"), Status = EnrollmentStatus.Dropped, CountsInGpa = false,
                EnrolledAt = now.AddDays(-12), DroppedAt = now.AddDays(-10),
            });
        await db.SaveChangesAsync(ct);

        // Enrollment audit log (rule E9).
        var csc210A = Sec("CSC 210", "A");
        var csc210B = Sec("CSC 210", "B");
        var mat201A = Sec("MAT 201", "A");
        var pastCsc101 = await db.Set<Section>().SingleAsync(s => s.Course.Code == "CSC 101" && !s.Term.IsCurrent && s.TermId != older.Id, ct);
        EnrollmentAction Act(EnrollmentActionType type, int? from, int? to, double daysAgo) => new()
        {
            StudentId = student, Action = type, FromSectionId = from, ToSectionId = to, PerformedBy = student, PerformedAt = now.AddDays(-daysAgo),
        };
        db.AddRange(
            Act(EnrollmentActionType.Enroll, null, oldMat101.Id, 445),
            Act(EnrollmentActionType.Enroll, null, pastCsc101.Id, 265),
            Act(EnrollmentActionType.Enroll, null, csc210B.Id, 12),
            Act(EnrollmentActionType.Swap, csc210B.Id, csc210A.Id, 10),
            Act(EnrollmentActionType.Enroll, null, mat201A.Id, 10));

        // Cart: a section that can be checked out, and one blocked by its prerequisite (CSC 310 needs CSC 210).
        db.AddRange(
            new CartItem { StudentId = student, Section = csc230A },
            new CartItem { StudentId = student, Section = Sec("CSC 310", "A") });

        // Notifications for the three demo accounts (some read, some not).
        Notification Note(int userId, NotificationType type, string title, string body, string link, double hoursAgo, bool read = false) => new()
        {
            UserId = userId, Type = type, Title = title, Body = body, Link = link,
            ReadAt = read ? now.AddHours(-hoursAgo + 1) : null,
        };
        db.AddRange(
            Note(student, NotificationType.Enrollment, "Section changed", "You moved from CSC 210 B to CSC 210 A.", "/student/schedule", 240, read: true),
            Note(student, NotificationType.Grade, "New grades posted", "CSC 210 A: Lab work and Midterm scores are available.", "/student/grades", 70),
            Note(student, NotificationType.AbsenceWarning, "Attendance warning", "You have 3 absences in MAT 201 A. The limit is 9.", "/student/attendance", 48),
            Note(student, NotificationType.Payment, "Payment received", "A payment of 250.00 was recorded for your registration fee.", "/student/finance", 30, read: true),
            Note(student, NotificationType.Payment, "Charge overdue", "CSC 210 lab fee (150.00) is past its due date.", "/student/finance", 20),
            Note(student, NotificationType.Support, "Support reply", "An admin replied to \"Can't add CSC 310 to my cart\".", "/student/support", 6),
            Note(instructor, NotificationType.General, "Attendance reminder", "Attendance for last week's CSC 101 A meetings is still open.", "/instructor/attendance", 30),
            Note(instructor, NotificationType.Grade, "Final scores pending", "CSC 210 A and MAT 201 A still have no Final scores entered.", "/instructor/grading", 12),
            Note(instructor, NotificationType.General, "New section assigned", "You were assigned CSC 230 A (Wednesday and Friday).", "/instructor/sections", 100, read: true),
            Note(admin, NotificationType.Support, "New support request", "Demo Student opened \"Profile photo doesn't upload\".", "/admin/support", 40),
            Note(admin, NotificationType.Payment, "Payments recorded", "3 payments were recorded against the tuition batch.", "/admin/payments", 90, read: true),
            Note(admin, NotificationType.General, "Term windows", "The add/drop window opens in 7 days.", "/admin/terms", 8));

        // Chat: a short finished conversation for Demo Student.
        var chat = new ChatConversation { StudentId = student, Title = "How many absences can I have?" };
        chat.Messages.Add(new ChatMessage { Role = ChatRole.User, Content = "How many absences can I have in a 3-credit course?" });
        chat.Messages.Add(new ChatMessage
        {
            Role = ChatRole.Assistant,
            Content = "The limit is three absences per credit, so 9 in a 3-credit course. At 8 you get a warning, at 9 a final notice, and at 10 you are dropped from the course with an F.",
        });
        db.Add(chat);

        await db.SaveChangesAsync(ct);

        // Finished career assessment for Demo Student in the current term (answers vary by question, scored by the real rules).
        var questions = await db.Set<CareerQuestion>().Where(q => q.IsActive)
            .Include(q => q.Options).ThenInclude(o => o.TraitPoints).OrderBy(q => q.Id).ToListAsync(ct);
        if (questions.Count >= AssessmentScoring.MinQuestions
            && !await db.Set<CareerAssessment>().AnyAsync(a => a.StudentId == student && a.TermId == current.Id, ct))
        {
            var quizQuestions = questions.Take(AssessmentScoring.MaxQuestions).ToList();
            var chosen = quizQuestions.Select((q, i) => (Question: q, Option: q.Options.OrderBy(o => o.OptionOrder).ElementAt(i * 3 % q.Options.Count))).ToList();
            var quiz = quizQuestions.Select(q => new QuizQuestion(q.Id, [.. q.Options.Select(o => new QuizOption(o.Id, o.TraitPoints.ToDictionary(t => t.TraitId, t => t.Points)))])).ToList();
            var scores = AssessmentScoring.Score(quiz, chosen.ToDictionary(c => c.Question.Id, c => c.Option.Id));

            var assessment = new CareerAssessment { StudentId = student, TermId = current.Id, SubmittedAt = now.AddDays(-4) };
            foreach (var (q, o) in chosen) assessment.Answers.Add(new CareerAssessmentAnswer { QuestionId = q.Id, OptionId = o.Id });
            foreach (var s in scores) assessment.TraitScores.Add(new CareerAssessmentTraitScore { TraitId = s.TraitId, Points = s.Points, MaxPoints = s.MaxPoints, Score = s.Score });
            db.Add(assessment);
        }
        await db.SaveChangesAsync(ct);

        // Rebuild the TermResults cache (rule G6) now that the older term has completed enrollments.
        var termResults = services.GetRequiredService<ITermResultService>();
        await termResults.RebuildAsync(student, ct);
        await termResults.RebuildAsync(student2, ct);
    }
}
