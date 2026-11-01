using SmartSis.Api.Features.Terms.Models.Entities;

namespace SmartSis.Api.Features.Terms.Models.Dtos;

/// <summary>A term with its two registration windows (all window times UTC).</summary>
/// <param name="Id">Term id.</param>
/// <param name="Name">Term name, e.g. "Fall 2026".</param>
/// <param name="StartDate">First day of the term.</param>
/// <param name="EndDate">Last day of the term.</param>
/// <param name="EnrollStart">Enrollment window opens (UTC).</param>
/// <param name="EnrollEnd">Enrollment window closes (UTC).</param>
/// <param name="AddDropStart">Add/Drop window opens (UTC).</param>
/// <param name="AddDropEnd">Add/Drop window closes (UTC).</param>
/// <param name="IsCurrent">True for the one current term.</param>
/// <param name="CurrentWindow">Window open right now: "Closed", "Enrollment" or "AddDrop".</param>
public sealed record TermDto(
    int Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTime EnrollStart,
    DateTime EnrollEnd,
    DateTime AddDropStart,
    DateTime AddDropEnd,
    bool IsCurrent,
    string CurrentWindow);

internal static class TermDtoMapping
{
    public static TermDto ToDto(this Term t, DateTime utcNow) => new(
        t.Id, t.Name, t.StartDate, t.EndDate,
        t.EnrollStart, t.EnrollEnd, t.AddDropStart, t.AddDropEnd,
        t.IsCurrent, t.WindowAt(utcNow).ToString());
}
