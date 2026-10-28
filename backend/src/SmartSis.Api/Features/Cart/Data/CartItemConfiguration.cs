using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Cart.Models.Entities;

namespace SmartSis.Api.Features.Cart.Data;

internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.ToTable("CartItems");
        b.HasIndex(x => new { x.StudentId, x.SectionId }).IsUnique(); // rule E7
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Section).WithMany().HasForeignKey(x => x.SectionId);
    }
}
