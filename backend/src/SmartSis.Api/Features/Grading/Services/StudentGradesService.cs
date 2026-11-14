using SmartSis.Api.Features.Enrollments.Enums;
using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Dtos;
using SmartSis.Api.Features.Grading.Rules;
using SmartSis.Api.Shared.Auth;

namespace SmartSis.Api.Features.Grading.Services;

/// <summary>The signed-in student's grades and transcript (PB-09). The student always comes from the token.</summary>
public sealed class StudentGradesService(IGradingRepository grading, ICurrentUser me) : IStudentGradesService
{
    public async Task<MyGradesDto> GetMineAsync(CancellationToken ct)
    {
        var studentId = me.UserId;
        var all = await grading.StudentCoursesAsync(studentId, null, ct);
        var cumulative = Cumulative(all);
        var term = await grading.GetCurrentTermAsync(ct);
        if (term is null) return new MyGradesDto(null, null, null, cumulative?.Gpa, cumulative?.CreditsEarned ?? 0, []);

        var rows = all.Where(r => r.TermId == term.Value.Id).ToList();
        var components = (await grading.TermComponentsAsync(term.Value.Id, [.. rows.Select(r => r.CourseId).Distinct()], ct))
            .ToLookup(c => c.CourseId, c => c.Component);
        var scores = (await grading.ScoresAsync([.. rows.Select(r => r.EnrollmentId)], ct))
            .ToDictionary(s => (s.EnrollmentId, s.ComponentId), s => s.Score);

        var courses = rows.Select(r =>
        {
            var parts = components[r.CourseId]
                .Select(c => (c, Score: scores.TryGetValue((r.EnrollmentId, c.Id), out var s) ? s : (decimal?)null))
                .ToList();
            var progress = GradeCalculator.Progress(parts.Select(p => new ComponentScore(p.c.MaxGrade, p.c.Weight, p.Score)));
            return new CourseGradesDto(
                r.SectionId, r.CourseCode, r.CourseName, r.SectionCode, r.Credits, r.Status,
                progress.GradedWeight, progress.Earned, progress.RunningPct, r.FinalPct, Final(r)?.LetterGrade, Final(r)?.GradePoints,
                [.. parts.Select(p => new ComponentGradeDto(
                    p.c.Name, p.c.Weight, p.c.MaxGrade, p.Score,
                    p.Score is { } s ? Math.Round(s / p.c.MaxGrade * 100, 2, MidpointRounding.AwayFromZero) : null))]);
        }).ToList();

        var graded = rows.Where(IsGraded).Select(ToAttempt).ToList();
        decimal? termGpa = graded.Count > 0 ? GpaCalculator.Gpa(graded) : null;
        return new MyGradesDto(term.Value.Id, term.Value.Name, termGpa, cumulative?.Gpa, cumulative?.CreditsEarned ?? 0, courses);
    }

    public async Task<TranscriptDto> GetTranscriptAsync(CancellationToken ct)
    {
        var studentId = me.UserId;
        var all = await grading.StudentCoursesAsync(studentId, null, ct);
        var stored = (await grading.ReadTermResultsAsync(studentId, ct)).ToDictionary(r => r.TermId);
        var order = TermOrder(all);
        // A term the cache doesn't have yet (e.g. an absence drop before the section is finalized) is computed the same way.
        var computed = GpaCalculator.ByTerm(all.Where(IsGraded).Select(ToAttempt), order).ToDictionary(r => r.TermId);

        var terms = all.GroupBy(r => r.TermId)
            .OrderBy(g => order.IndexOf(g.Key))
            .Select(g =>
            {
                var first = g.First();
                var courses = g.Select(r => new TranscriptCourseDto(
                    r.CourseCode, r.CourseName, r.SectionCode, r.Credits, r.Status, r.FinalPct,
                    Final(r)?.LetterGrade, Final(r)?.GradePoints, r.CountsInGpa)).ToList();
                if (stored.TryGetValue(g.Key, out var s))
                    return new TranscriptTermDto(g.Key, first.TermName, first.TermIsCurrent, s.TermGpa, s.CumulativeGpa, s.CreditsAttempted, s.CreditsEarned, courses);
                if (computed.TryGetValue(g.Key, out var c))
                    return new TranscriptTermDto(g.Key, first.TermName, first.TermIsCurrent, c.TermGpa, c.CumulativeGpa, c.CreditsAttempted, c.CreditsEarned, courses);
                return new TranscriptTermDto(g.Key, first.TermName, first.TermIsCurrent, null, null, 0, 0, courses);
            })
            .ToList();

        var cumulative = Cumulative(all);
        return new TranscriptDto(cumulative?.Gpa, cumulative?.CreditsAttempted ?? 0, cumulative?.CreditsEarned ?? 0, terms);
    }

    /// <summary>Completed, or dropped for absences (F, 0.0, rule A3): both count in the GPA.</summary>
    private static bool IsGraded(StudentCourseRow r) =>
        r.GradePoints is not null && r.Status is EnrollmentStatus.Completed or EnrollmentStatus.DroppedAbsence;

    private static (string? LetterGrade, decimal? GradePoints)? Final(StudentCourseRow r) =>
        IsGraded(r) ? (r.LetterGrade, r.GradePoints) : null;

    private static GradedAttempt ToAttempt(StudentCourseRow r) => new(r.TermId, r.CourseId, r.Credits, r.GradePoints!.Value);

    private static List<int> TermOrder(IEnumerable<StudentCourseRow> rows) =>
        [.. rows.Select(r => (r.TermId, r.TermStart)).Distinct().OrderBy(t => t.TermStart).ThenBy(t => t.TermId).Select(t => t.TermId)];

    /// <summary>Rules G4 + G5 over every graded attempt; null when the student has none.</summary>
    private static CumulativeGpa? Cumulative(List<StudentCourseRow> all)
    {
        var graded = all.Where(IsGraded).ToList();
        return graded.Count == 0 ? null : GpaCalculator.Cumulative(graded.Select(ToAttempt), TermOrder(graded));
    }
}
