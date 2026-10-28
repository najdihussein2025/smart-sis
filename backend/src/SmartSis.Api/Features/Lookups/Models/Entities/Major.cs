using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Lookups.Models.Entities;

public class Major : BaseEntity
{
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = "";

    /// <summary>Credit cap per term for students of this major (rule E2). Default 18.</summary>
    public int DefaultMaxCredits { get; set; } = 18;
}
