using SmartSis.Api.Features.Terms.Enums;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Terms.Models.Entities;

public class Term : BaseEntity
{
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Enrollment window (UTC): enroll + swap, no drop.</summary>
    public DateTime EnrollStart { get; set; }
    public DateTime EnrollEnd { get; set; }

    /// <summary>Add/Drop window (UTC): add + drop + swap.</summary>
    public DateTime AddDropStart { get; set; }
    public DateTime AddDropEnd { get; set; }

    public bool IsCurrent { get; set; }

    public TermWindow WindowAt(DateTime utcNow)
    {
        if (utcNow >= AddDropStart && utcNow < AddDropEnd) return TermWindow.AddDrop;
        if (utcNow >= EnrollStart && utcNow < EnrollEnd) return TermWindow.Enrollment;
        return TermWindow.Closed;
    }
}
