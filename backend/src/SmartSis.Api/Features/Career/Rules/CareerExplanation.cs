using System.Text;

namespace SmartSis.Api.Features.Career.Rules;

/// <summary>A matched career with its strongest trait names, for the explanation.</summary>
public sealed record ExplainedCareer(string Name, int MatchPct, IReadOnlyList<string> TopTraits);

/// <summary>
/// The explanation shown when the AI service can't write one (not configured, down, slow or refusing). Deterministic,
/// so recommendations never fail because of the LLM (PB-17 / T66).
/// </summary>
public static class CareerExplanation
{
    public static string Fallback(IReadOnlyList<ExplainedCareer> careers, bool hasGrades)
    {
        if (careers.Count == 0)
            return "We couldn't match you to a career yet because no career paths are set up. Please check again later.";

        var sb = new StringBuilder();
        var best = careers[0];
        sb.Append($"Your strongest match is {best.Name} ({best.MatchPct}%)");
        sb.Append(best.TopTraits.Count > 0 ? $", driven mostly by {Join(best.TopTraits)}." : ".");
        if (careers.Count > 1)
            sb.Append(" You also fit ")
              .Append(Join(careers.Skip(1).Select(c => $"{c.Name} ({c.MatchPct}%)").ToList()))
              .Append('.');
        sb.Append(hasGrades
            ? " These scores combine your assessment answers (about 70%) with your grades in related courses (about 30%)."
            : " These scores come from your assessment answers; grades in related courses will refine them once you complete those courses.");
        sb.Append(" Follow a career's roadmap and add its next course to your cart.");
        return sb.ToString();
    }

    private static string Join(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        2 => $"{items[0]} and {items[1]}",
        _ => $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}",
    };
}
