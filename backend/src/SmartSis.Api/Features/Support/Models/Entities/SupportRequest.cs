using SmartSis.Api.Features.Support.Enums;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Models.Entities;

/// <summary>Submitted by a student, handled by admins (rule C3).</summary>
public class SupportRequest : BaseEntity
{
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CategoryId { get; set; }
    public SupportCategory Category { get; set; } = null!;
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public SupportStatus Status { get; set; } = SupportStatus.Open;

    /// <summary>Users.Id of the admin handling it.</summary>
    public int? AssignedAdminId { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public ICollection<SupportMessage> Messages { get; set; } = [];
}
