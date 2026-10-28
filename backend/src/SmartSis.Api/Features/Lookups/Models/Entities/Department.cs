using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Lookups.Models.Entities;

public class Department : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
