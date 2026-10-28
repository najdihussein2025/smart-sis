namespace SmartSis.Api.Features.Attendance.Enums;

/// <summary>Rule A2/A3: Warning at limit − 1, Limit at limit, AutoDropped at limit + 1.</summary>
public enum AbsenceAlertLevel
{
    Warning,
    Limit,
    AutoDropped,
}
