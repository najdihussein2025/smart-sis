using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Terms.Models.Entities;

namespace SmartSis.Api.Features.Terms.Data;

internal sealed class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> b)
    {
        b.ToTable("Terms", t =>
        {
            t.HasCheckConstraint("CK_Terms_Dates", "[StartDate] < [EndDate]");
            t.HasCheckConstraint("CK_Terms_EnrollWindow", "[EnrollStart] < [EnrollEnd]");
            t.HasCheckConstraint("CK_Terms_AddDropWindow", "[AddDropStart] < [AddDropEnd]");
        });
        b.Property(x => x.Name).HasMaxLength(64);
        b.HasIndex(x => x.Name).IsUnique();
        // At most one current term.
        b.HasIndex(x => x.IsCurrent).IsUnique().HasFilter("[IsCurrent] = 1");
    }
}
