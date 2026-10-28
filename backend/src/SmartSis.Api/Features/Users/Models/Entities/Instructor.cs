using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Users.Models.Entities;

/// <summary>Instructor profile. PK = UserId (1:1 with Users).</summary>
public class Instructor : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
}
