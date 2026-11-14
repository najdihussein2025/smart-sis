using Microsoft.EntityFrameworkCore;
using SmartSis.Api.Features.Career.Models.Entities;
using SmartSis.Api.Features.Courses.Models.Entities;

namespace SmartSis.Api.Shared.Data.Seed;

/// <summary>
/// Career module demo data (PB-16, PB-17, PB-20): 6 traits, 6 careers with weights and roadmaps, 24 questions,
/// and trait tags on CSC 101, CSC 210, CSC 310 and MAT 201. No assessments: every student can take the quiz.
/// </summary>
public sealed class CareerDevSeed : IDevSeedStep
{
    public int Order => 600;

    private const string Analytical = "Analytical thinking";
    private const string Creativity = "Creativity";
    private const string Communication = "Communication";
    private const string Leadership = "Leadership";
    private const string Technical = "Technical aptitude";
    private const string Detail = "Attention to detail";

    public async Task SeedAsync(SisDbContext db, IServiceProvider services, CancellationToken ct)
    {
        if (await db.Set<CareerTrait>().AnyAsync(ct)) return;

        var traits = new[]
        {
            new CareerTrait { Name = Analytical, Description = "Breaking problems down and reasoning with data and logic." },
            new CareerTrait { Name = Creativity, Description = "Coming up with new ideas, designs and approaches." },
            new CareerTrait { Name = Communication, Description = "Explaining ideas clearly and working with people." },
            new CareerTrait { Name = Leadership, Description = "Organizing work, deciding and guiding a team." },
            new CareerTrait { Name = Technical, Description = "Enjoying building and understanding technical systems." },
            new CareerTrait { Name = Detail, Description = "Being careful, precise and thorough." },
        }.ToDictionary(t => t.Name);
        db.AddRange(traits.Values);

        var courses = await db.Set<Course>().Where(c => new[] { "CSC 101", "CSC 210", "CSC 310", "MAT 201" }.Contains(c.Code))
            .ToDictionaryAsync(c => c.Code, ct);
        Course? C(string code) => courses.GetValueOrDefault(code);

        // Course tags: grades in these courses count towards the traits (~30% of the match).
        void Tag(string code, params string[] names)
        {
            if (C(code) is not { } course) return;
            foreach (var n in names) db.Add(new CourseTrait { Course = course, Trait = traits[n] });
        }
        Tag("CSC 101", Technical, Analytical);
        Tag("CSC 210", Analytical, Technical, Detail);
        Tag("CSC 310", Detail, Analytical);
        Tag("MAT 201", Analytical, Detail);

        // Careers with trait weights and roadmaps.
        CareerPath Career(string name, string description, (string Trait, int Weight)[] weights, (string Title, string? Description, string? Course)[] steps)
        {
            var career = new CareerPath { Name = name, Description = description };
            foreach (var (t, w) in weights) career.Traits.Add(new CareerPathTrait { Trait = traits[t], Weight = w });
            var order = 1;
            foreach (var (title, desc, code) in steps)
                career.Steps.Add(new CareerRoadmapStep { StepOrder = order++, Title = title, Description = desc, Course = code is null ? null : C(code) });
            return career;
        }

        db.AddRange(
            Career("Software Engineer", "Designs, builds and maintains software applications.",
                [(Technical, 10), (Analytical, 8), (Detail, 5), (Creativity, 4), (Communication, 3)],
                [
                    ("Learn to program", "Variables, control flow and functions.", "CSC 101"),
                    ("Master data structures", "Choose the right structure for each problem.", "CSC 210"),
                    ("Think in logic and graphs", "Discrete maths behind algorithms.", "MAT 201"),
                    ("Persist data", "Model and query relational databases.", "CSC 310"),
                    ("Build a portfolio project", "Ship a small app end to end and publish the code.", null),
                ]),
            Career("Data Scientist", "Turns data into insight with statistics, programming and models.",
                [(Analytical, 10), (Technical, 7), (Detail, 6), (Communication, 5)],
                [
                    ("Learn to program", "Python-style thinking for data work.", "CSC 101"),
                    ("Learn the maths", "Logic, sets and combinatorics.", "MAT 201"),
                    ("Work with databases", "SQL is the daily tool of a data scientist.", "CSC 310"),
                    ("Analyse a real dataset", "Clean, explore and present a public dataset.", null),
                ]),
            Career("Database Administrator", "Keeps databases fast, safe and available.",
                [(Detail, 10), (Technical, 8), (Analytical, 6), (Leadership, 2)],
                [
                    ("Learn to program", null, "CSC 101"),
                    ("Data structures", "Indexes are data structures.", "CSC 210"),
                    ("Database systems", "SQL, normalization and transactions.", "CSC 310"),
                    ("Practise backups and tuning", "Set up a test server and restore a backup.", null),
                ]),
            Career("UX Designer", "Designs products that are easy and pleasant to use.",
                [(Creativity, 10), (Communication, 8), (Detail, 5), (Technical, 3)],
                [
                    ("Learn to program", "Know what developers can build.", "CSC 101"),
                    ("Run user interviews", "Talk to five users about a problem they have.", null),
                    ("Build a design portfolio", "Three case studies from problem to prototype.", null),
                ]),
            Career("IT Project Manager", "Plans and leads technology projects and teams.",
                [(Leadership, 10), (Communication, 9), (Analytical, 4), (Detail, 4)],
                [
                    ("Learn to program", "Understand the work your team does.", "CSC 101"),
                    ("Data structures", "Speak the same language as engineers.", "CSC 210"),
                    ("Lead a student project", "Organize a team, a plan and a deadline.", null),
                ]),
            Career("Cybersecurity Analyst", "Protects systems and data from attacks.",
                [(Technical, 9), (Detail, 9), (Analytical, 7), (Communication, 3)],
                [
                    ("Learn to program", null, "CSC 101"),
                    ("Discrete mathematics", "The logic behind cryptography.", "MAT 201"),
                    ("Data structures", null, "CSC 210"),
                    ("Join a capture-the-flag event", "Practise finding and fixing vulnerabilities.", null),
                ]));

        // Question bank: 24 questions, 4 options each; every option adds points to one or two traits.
        var bank = new (string Text, (string Option, (string Trait, int Points)[] Points)[] Options)[]
        {
            ("In a group project, which task do you pick first?", [
                ("Planning who does what", [(Leadership, 3)]),
                ("Writing the code", [(Technical, 3)]),
                ("Designing the look and feel", [(Creativity, 3)]),
                ("Presenting the result", [(Communication, 3)])]),
            ("A puzzle you can't solve right away makes you…", [
                ("Break it into smaller parts", [(Analytical, 3)]),
                ("Try an unusual idea", [(Creativity, 3)]),
                ("Ask someone to think it through with you", [(Communication, 2)]),
                ("Check every step for a mistake", [(Detail, 3)])]),
            ("Which school subject did you enjoy most?", [
                ("Mathematics", [(Analytical, 3)]),
                ("Art or design", [(Creativity, 3)]),
                ("Languages or debate", [(Communication, 3)]),
                ("Physics or technology", [(Technical, 3)])]),
            ("Your ideal weekend project is…", [
                ("Building a small app or gadget", [(Technical, 3), (Creativity, 1)]),
                ("Organizing an event with friends", [(Leadership, 3)]),
                ("Sketching or making videos", [(Creativity, 3)]),
                ("Solving logic or number puzzles", [(Analytical, 3)])]),
            ("When you read instructions, you…", [
                ("Follow them exactly, step by step", [(Detail, 3)]),
                ("Skim them and start experimenting", [(Creativity, 2), (Technical, 1)]),
                ("Look for why each step is needed", [(Analytical, 2)]),
                ("Explain them to others afterwards", [(Communication, 2)])]),
            ("A teammate is stuck. You…", [
                ("Help them debug the problem", [(Technical, 2), (Analytical, 1)]),
                ("Reorganize the work so the team still finishes", [(Leadership, 3)]),
                ("Talk it through and encourage them", [(Communication, 3)]),
                ("Review their work for small errors", [(Detail, 3)])]),
            ("Which compliment would make you proudest?", [
                ("\"Your solution is so clever.\"", [(Analytical, 3)]),
                ("\"That design is beautiful.\"", [(Creativity, 3)]),
                ("\"You explained it perfectly.\"", [(Communication, 3)]),
                ("\"You kept us all on track.\"", [(Leadership, 3)])]),
            ("How do you feel about spreadsheets full of numbers?", [
                ("I love finding patterns in them", [(Analytical, 3)]),
                ("I like making sure every value is right", [(Detail, 3)]),
                ("I'd rather turn them into a chart", [(Creativity, 2), (Communication, 1)]),
                ("I prefer someone else does it", [(Leadership, 1)])]),
            ("When a computer misbehaves, you…", [
                ("Open settings and fix it yourself", [(Technical, 3)]),
                ("Search methodically for the cause", [(Analytical, 2), (Detail, 1)]),
                ("Ask for help clearly", [(Communication, 2)]),
                ("Find a creative workaround", [(Creativity, 2)])]),
            ("In meetings you usually…", [
                ("Lead the discussion", [(Leadership, 3)]),
                ("Take careful notes", [(Detail, 3)]),
                ("Suggest new ideas", [(Creativity, 3)]),
                ("Make sure everyone understands", [(Communication, 3)])]),
            ("Which job task sounds most fun?", [
                ("Writing a program that automates boring work", [(Technical, 3)]),
                ("Finding the cause of a drop in sales", [(Analytical, 3)]),
                ("Designing a new app screen", [(Creativity, 3)]),
                ("Pitching a product to clients", [(Communication, 3)])]),
            ("A deadline is close and plans changed. You…", [
                ("Re-plan and assign the work", [(Leadership, 3)]),
                ("Focus and get your part done well", [(Detail, 2), (Technical, 1)]),
                ("Look for a smarter shortcut", [(Creativity, 2), (Analytical, 1)]),
                ("Update everyone on the new plan", [(Communication, 3)])]),
            ("How do you prefer to learn something new?", [
                ("Build something with it", [(Technical, 3)]),
                ("Understand the theory first", [(Analytical, 3)]),
                ("Discuss it with others", [(Communication, 3)]),
                ("Make my own notes and diagrams", [(Creativity, 1), (Detail, 2)])]),
            ("Which would you rather fix?", [
                ("A slow database query", [(Technical, 2), (Detail, 1)]),
                ("A confusing user interface", [(Creativity, 3)]),
                ("A team that doesn't communicate", [(Leadership, 2), (Communication, 1)]),
                ("A wrong calculation in a report", [(Analytical, 1), (Detail, 2)])]),
            ("You notice a small typo in a published report. You…", [
                ("Fix it right away, it matters", [(Detail, 3)]),
                ("Mention it politely to the author", [(Communication, 2)]),
                ("Check whether the numbers are right too", [(Analytical, 3)]),
                ("Ignore it, the big picture matters more", [(Leadership, 1), (Creativity, 1)])]),
            ("What kind of game do you enjoy?", [
                ("Strategy games", [(Analytical, 2), (Leadership, 1)]),
                ("Building or sandbox games", [(Creativity, 3)]),
                ("Team games with lots of talking", [(Communication, 3)]),
                ("Puzzle games with exact rules", [(Detail, 2), (Analytical, 1)])]),
            ("How do you organize your files?", [
                ("Strict folders and names", [(Detail, 3)]),
                ("Scripts that sort them for me", [(Technical, 3)]),
                ("Visually, with colours and icons", [(Creativity, 2)]),
                ("I don't, I just search", [(Analytical, 1)])]),
            ("Your friends usually come to you for…", [
                ("Tech help", [(Technical, 3)]),
                ("Advice and decisions", [(Leadership, 3)]),
                ("Creative ideas", [(Creativity, 3)]),
                ("A good listener", [(Communication, 3)])]),
            ("Which statement sounds most like you?", [
                ("I like knowing how things work inside", [(Technical, 3)]),
                ("I like proving things are true", [(Analytical, 3)]),
                ("I like making things look and feel good", [(Creativity, 3)]),
                ("I like getting people to agree", [(Leadership, 2), (Communication, 1)])]),
            ("When writing an essay, your strength is…", [
                ("A clear structure and argument", [(Analytical, 2), (Communication, 1)]),
                ("An original angle", [(Creativity, 3)]),
                ("No spelling or citation mistakes", [(Detail, 3)]),
                ("Finishing early and helping others", [(Leadership, 2)])]),
            ("Which future workplace appeals to you?", [
                ("A tech company building products", [(Technical, 3)]),
                ("A research lab", [(Analytical, 3)]),
                ("A design studio", [(Creativity, 3)]),
                ("Leading my own team or company", [(Leadership, 3)])]),
            ("When something goes wrong in a project, you first…", [
                ("Find the root cause", [(Analytical, 3)]),
                ("Tell the people affected", [(Communication, 3)]),
                ("Take charge of the recovery", [(Leadership, 3)]),
                ("Check what was missed and add a checklist", [(Detail, 3)])]),
            ("How do you feel about security and privacy settings?", [
                ("I configure them carefully", [(Detail, 2), (Technical, 1)]),
                ("I like learning how attacks work", [(Technical, 3)]),
                ("I explain them to my family", [(Communication, 2)]),
                ("I rarely think about them", [(Creativity, 1)])]),
            ("Pick the tool you'd most like to master.", [
                ("A programming language", [(Technical, 3)]),
                ("A statistics package", [(Analytical, 3)]),
                ("A design tool", [(Creativity, 3)]),
                ("A project-planning tool", [(Leadership, 2), (Detail, 1)])]),
        };

        foreach (var (text, options) in bank)
        {
            var question = new CareerQuestion { Text = text };
            var order = 1;
            foreach (var (optionText, points) in options)
            {
                var option = new CareerQuestionOption { Text = optionText, OptionOrder = order++ };
                foreach (var (t, p) in points) option.TraitPoints.Add(new CareerOptionTraitPoint { Trait = traits[t], Points = p });
                question.Options.Add(option);
            }
            db.Add(question);
        }
    }
}
