using SmartSis.Api.Features.Sections.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;
using SmartSis.Api.Shared.Models;

namespace SmartSis.Api.Features.Cart.Models.Entities;

public class CartItem : BaseEntity
{
    /// <summary>Students.UserId.</summary>
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int SectionId { get; set; }
    public Section Section { get; set; } = null!;
}
