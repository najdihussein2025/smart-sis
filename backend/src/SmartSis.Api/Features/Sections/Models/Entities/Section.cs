using SmartSis.Api.Features.Courses.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Sections.Models.Entities;

/// <summary>One offering of a course in a term. Seats taken are counted from Enrollments, never stored here (rule E8).</summary>
public class Section : BaseEntity
{
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public int TermId { get; set; }
    public Term Term { get; set; } = null!;

    public string SectionCode { get; set; } = "";

    /// <summary>Instructors.UserId. Null until the admin assigns one.</summary>
    public int? InstructorId { get; set; }
    public Instructor? Instructor { get; set; }

    public int Capacity { get; set; }

    public ICollection<SectionMeeting> Meetings { get; set; } = [];

    public bool IsTaughtBy(int userId) => InstructorId == userId;
}
