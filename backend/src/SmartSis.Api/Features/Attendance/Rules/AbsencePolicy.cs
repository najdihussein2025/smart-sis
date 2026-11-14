using SmartSis.Api.Features.Attendance.Enums;

namespace SmartSis.Api.Features.Attendance.Rules;

/// <summary>What to do for one enrollment after an absence: the alert to send now (if any) and whether to auto-drop.</summary>
/// <param name="Alert">The level to record and send now, or null when nothing is due (or it was already sent).</param>
/// <param name="AutoDrop">True at limit + 1 or more (rule A3).</param>
public sealed record AbsenceDecision(AbsenceAlertLevel? Alert, bool AutoDrop)
{
    public static readonly AbsenceDecision None = new(null, false);
}

/// <summary>
/// Rules A1, A2, A3 and A5. Pure: no database, no clock.
/// <list type="bullet">
/// <item>A1: limit = credits × 3.</item>
/// <item>A2: Warning at limit − 1, Limit at the limit; each one once per enrollment (the caller passes what was already sent).</item>
/// <item>A3: at limit + 1 the student is dropped (DroppedAbsence, F, 0.0, counts in GPA) and told once (AutoDropped).</item>
/// <item>A5: 0 → Perfect, 1 … limit − 2 → Warning, ≥ limit − 1 → Critical.</item>
/// </list>
/// </summary>
public static class AbsencePolicy
{
    public const int AbsencesPerCredit = 3;

    /// <summary>Rule A1.</summary>
    public static int LimitFor(int credits) => credits * AbsencesPerCredit;

    /// <summary>Rule A5.</summary>
    public static AbsenceStanding StandingFor(int absences, int limit) =>
        absences <= 0 ? AbsenceStanding.Perfect
        : absences >= limit - 1 ? AbsenceStanding.Critical
        : AbsenceStanding.Warning;

    /// <summary>
    /// Rules A2 and A3 for an enrollment that now has <paramref name="absences"/> absences.
    /// Only the level matching the count is considered: a submission adds at most one absence per student, so each
    /// threshold is crossed on its own. A level in <paramref name="alreadySent"/> is never sent again.
    /// </summary>
    public static AbsenceDecision Evaluate(int absences, int limit, IReadOnlyCollection<AbsenceAlertLevel> alreadySent)
    {
        if (absences > limit)
            return new AbsenceDecision(alreadySent.Contains(AbsenceAlertLevel.AutoDropped) ? null : AbsenceAlertLevel.AutoDropped, true);

        var level = absences == limit ? AbsenceAlertLevel.Limit
            : absences == limit - 1 ? AbsenceAlertLevel.Warning
            : (AbsenceAlertLevel?)null;

        return level is { } l && !alreadySent.Contains(l) ? new AbsenceDecision(l, false) : AbsenceDecision.None;
    }
}
