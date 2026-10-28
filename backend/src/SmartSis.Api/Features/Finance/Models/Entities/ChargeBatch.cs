using SmartSis.Api.Features.Lookups.Models.Entities;
using SmartSis.Api.Features.Terms.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Models.Entities;

/// <summary>"Charge every student of a major" in one action (rule F1).</summary>
public class ChargeBatch : BaseEntity
{
    public int MajorId { get; set; }
    public Major Major { get; set; } = null!;
    public int TermId { get; set; }
    public Term Term { get; set; } = null!;
    public int PaymentTypeId { get; set; }
    public PaymentType PaymentType { get; set; } = null!;
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }

    /// <summary>Users.Id of the admin.</summary>
    public int CreatedBy { get; set; }
}
