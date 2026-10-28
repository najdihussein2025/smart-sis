using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Grading.Models.Entities;

/// <summary>Defined by the admin per course per term. Σ Weight for a course+term ≤ 100 (rule G1).</summary>
public class GradeComponent : BaseEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int TermId { get; set; }
    public Term Term { get; set; } = null!;
    public string Name { get; set; } = "";
    public decimal MaxGrade { get; set; }
    public decimal Weight { get; set; }
}
