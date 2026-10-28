using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Lookups.Models.Entities;

/// <summary>Percentage → letter → GPA points. Seeded from PROJECT.md §4.</summary>
public class GradeScale : BaseEntity
{
    public string Letter { get; set; } = "";
    public string StatusLabel { get; set; } = "";
    public decimal GpaPoints { get; set; }
    public int MinPct { get; set; }
    public int MaxPct { get; set; }
}
