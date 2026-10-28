using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartSis.Api.Features.Finance.Models.Entities;
using SmartSis.Api.Features.Users.Models.Entities;

namespace SmartSis.Api.Features.Finance.Data;

internal sealed class ChargeBatchConfiguration : IEntityTypeConfiguration<ChargeBatch>
{
    public void Configure(EntityTypeBuilder<ChargeBatch> b)
    {
        b.ToTable("ChargeBatches", t => t.HasCheckConstraint("CK_ChargeBatches_Amount", "[Amount] > 0"));
        b.Property(x => x.Title).HasMaxLength(128);
        b.HasOne(x => x.Major).WithMany().HasForeignKey(x => x.MajorId);
        b.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId);
        b.HasOne(x => x.PaymentType).WithMany().HasForeignKey(x => x.PaymentTypeId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy);
    }
}

internal sealed class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> b)
    {
        b.ToTable("Charges", t => t.HasCheckConstraint("CK_Charges_Amount", "[Amount] > 0"));
        b.Property(x => x.Title).HasMaxLength(128);
        b.HasIndex(x => new { x.StudentId, x.Status });
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Batch).WithMany().HasForeignKey(x => x.BatchId).IsRequired(false);
        b.HasOne(x => x.Term).WithMany().HasForeignKey(x => x.TermId);
        b.HasOne(x => x.PaymentType).WithMany().HasForeignKey(x => x.PaymentTypeId);
        b.Ignore(x => x.PaidAmount);
        b.Ignore(x => x.Balance);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments", t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0"));
        b.Property(x => x.Note).HasMaxLength(512);
        b.HasOne(x => x.Charge).WithMany(c => c.Payments).HasForeignKey(x => x.ChargeId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RecordedBy);
    }
}
