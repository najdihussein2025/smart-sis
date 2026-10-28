using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Support.Models.Entities;

public class SupportMessage : BaseEntity
{
    public int RequestId { get; set; }
    public SupportRequest Request { get; set; } = null!;
    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Body { get; set; } = "";
}
