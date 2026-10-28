using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Finance.Models.Entities;

public class Payment : BaseEntity
{
    public int ChargeId { get; set; }
    public Charge Charge { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }

    /// <summary>Users.Id of the admin who recorded it.</summary>
    public int RecordedBy { get; set; }
    public string? Note { get; set; }
}
