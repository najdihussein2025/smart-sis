using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Courses.Models.Entities;

/// <summary>PK = (CourseId, PrerequisiteId).</summary>
public class CoursePrerequisite : AuditableEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int PrerequisiteId { get; set; }
    public Course Prerequisite { get; set; } = null!;
}
