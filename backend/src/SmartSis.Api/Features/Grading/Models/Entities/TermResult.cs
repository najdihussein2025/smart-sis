using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Grading.Models.Entities;

/// <summary>Cache of GPA per student per term. PK = (StudentId, TermId). Rebuilt, never edited by hand (rule G6).</summary>
public class TermResult : AuditableEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int TermId { get; set; }
    public Term Term { get; set; } = null!;
    public decimal TermGpa { get; set; }
    public decimal CumulativeGpa { get; set; }
    public int CreditsAttempted { get; set; }
    public int CreditsEarned { get; set; }
}
