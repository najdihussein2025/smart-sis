namespace SmartSis.Api.Features.Terms.Models.Dtos;

/// <summary>Create or update a term (same body for POST and PUT).</summary>
/// <param name="Name">Unique name, e.g. "Fall 2026" (max 64, trimmed, case-insensitive unique).</param>
/// <param name="StartDate">First day of the term.</param>
/// <param name="EndDate">Last day of the term. Must be after <paramref name="StartDate"/>.</param>
/// <param name="EnrollStart">Enrollment window opens (UTC, ending in Z). Students enroll and swap; no drop.</param>
/// <param name="EnrollEnd">Enrollment window closes (UTC). Must be after <paramref name="EnrollStart"/> and no later than <paramref name="AddDropStart"/>.</param>
/// <param name="AddDropStart">Add/Drop window opens (UTC). Students add, drop and swap.</param>
/// <param name="AddDropEnd">Add/Drop window closes (UTC). Must be after <paramref name="AddDropStart"/> and on or before <paramref name="EndDate"/>.</param>
/// <param name="IsCurrent">Make this the current term. The previous current term is unset automatically.</param>
public sealed record SaveTermRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTime EnrollStart,
    DateTime EnrollEnd,
    DateTime AddDropStart,
    DateTime AddDropEnd,
    bool IsCurrent);
