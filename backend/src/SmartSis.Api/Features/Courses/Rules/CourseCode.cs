using System.Text.RegularExpressions;

namespace SmartSis.Api.Features.Courses.Rules;

/// <summary>Course code format: 2–5 letters, optional space, 3 digits, optional letter suffix ("CSC 101", "mat201", "PHY 110L").</summary>
public static partial class CourseCode
{
    [GeneratedRegex(@"^(?<dept>[A-Za-z]{2,5})\s?(?<num>\d{3}[A-Za-z]?)$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? code) => code is not null && Pattern().IsMatch(code.Trim());

    /// <summary>Upper case with exactly one space: "csc101" → "CSC 101". Throws if the code isn't valid.</summary>
    public static string Normalize(string code)
    {
        var m = Pattern().Match(code.Trim());
        if (!m.Success) throw new ArgumentException($"'{code}' is not a valid course code.", nameof(code));
        return $"{m.Groups["dept"].Value.ToUpperInvariant()} {m.Groups["num"].Value.ToUpperInvariant()}";
    }
}
