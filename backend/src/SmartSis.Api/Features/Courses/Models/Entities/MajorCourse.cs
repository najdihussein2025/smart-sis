using SmartSis.Api.Features.Courses.Enums;
using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Courses.Models.Entities;

/// <summary>Course ↔ major mapping. PK = (MajorId, CourseId). The same course can be Major for one major and Elective for another.</summary>
public class MajorCourse : AuditableEntity
{
    public int MajorId { get; set; }
    public Major Major { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public CourseType Type { get; set; }
}
