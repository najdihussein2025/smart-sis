using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Lookups.Models.Entities;

public class Room : BaseEntity
{
    public int CampusId { get; set; }
    public Campus Campus { get; set; } = null!;
    public string Name { get; set; } = "";
    public int Capacity { get; set; }
}
