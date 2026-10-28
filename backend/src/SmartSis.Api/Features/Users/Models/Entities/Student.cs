using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Users.Models.Entities;

/// <summary>Student profile. PK = UserId (1:1 with Users).</summary>
public class Student : AuditableEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int MajorId { get; set; }
    public Major Major { get; set; } = null!;

    public int CampusId { get; set; }
    public Campus Campus { get; set; } = null!;

    public DateOnly EnrollmentDate { get; set; }

    /// <summary>Admin override of the major's credit cap. Null = use Majors.DefaultMaxCredits.</summary>
    public int? MaxCreditsOverride { get; set; }

    /// <summary>Rule E2.</summary>
    public int EffectiveMaxCredits(int majorDefault) => MaxCreditsOverride ?? majorDefault;
}
