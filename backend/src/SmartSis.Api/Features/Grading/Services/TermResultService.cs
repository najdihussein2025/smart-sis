using SmartSis.Api.Features.Grading.Interfaces;
using SmartSis.Api.Features.Grading.Models.Entities;
using SmartSis.Api.Features.Grading.Rules;

namespace SmartSis.Api.Features.Grading.Services;

public sealed class TermResultService(IGradingRepository grading) : ITermResultService
{
    public async Task RebuildAsync(int studentId, CancellationToken ct)
    {
        var rows = await grading.GradedAttemptsAsync(studentId, ct);
        var order = TermOrder(rows);
        var fresh = GpaCalculator.ByTerm(rows.Select(r => r.Attempt), order).ToDictionary(r => r.TermId);

        var existing = await grading.TermResultsAsync(studentId, ct);
        foreach (var old in existing.Where(e => !fresh.ContainsKey(e.TermId)))
            grading.Remove(old);

        foreach (var r in fresh.Values)
        {
            var row = existing.FirstOrDefault(e => e.TermId == r.TermId);
            if (row is null)
            {
                row = new TermResult { StudentId = studentId, TermId = r.TermId };
                grading.Add(row);
            }
            row.TermGpa = r.TermGpa;
            row.CumulativeGpa = r.CumulativeGpa;
            row.CreditsAttempted = r.CreditsAttempted;
            row.CreditsEarned = r.CreditsEarned;
        }
    }

    /// <summary>Term ids oldest first (by start date, then id).</summary>
    internal static List<int> TermOrder(IEnumerable<AttemptRow> rows) =>
        [.. rows.Select(r => (r.Attempt.TermId, r.TermStart)).Distinct().OrderBy(t => t.TermStart).ThenBy(t => t.TermId).Select(t => t.TermId)];
}
