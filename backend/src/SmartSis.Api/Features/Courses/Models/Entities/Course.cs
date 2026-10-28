using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Courses.Models.Entities;

public class Course : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Credits { get; set; }
    public string? Description { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>Courses that must be passed (D or better) before this one.</summary>
    public ICollection<CoursePrerequisite> Prerequisites { get; set; } = [];
    public ICollection<MajorCourse> MajorCourses { get; set; } = [];

    /// <summary>Rule A1: absence limit = credits × 3.</summary>
    public int AbsenceLimit => Credits * 3;
}
