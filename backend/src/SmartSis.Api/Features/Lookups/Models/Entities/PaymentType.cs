using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Lookups.Models.Entities;

/// <summary>Tuition, Registration, Lab fee…</summary>
public class PaymentType : BaseEntity
{
    public string Name { get; set; } = "";
}
