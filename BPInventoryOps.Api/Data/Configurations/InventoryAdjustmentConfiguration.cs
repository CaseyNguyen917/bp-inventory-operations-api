using BPInventoryOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BPInventoryOps.Api.Data.Configurations;

public class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable(
            "InventoryAdjustments",
            table => table.HasCheckConstraint(
                "CK_InventoryAdjustments_QuantityChange_NonZero",
                "[QuantityChange] <> 0"));

        builder.Property(adjustment => adjustment.RecordedByUserId)
            .IsRequired();

        builder.Property(adjustment => adjustment.Reason)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(adjustment => adjustment.Notes)
            .HasMaxLength(1000);

        builder.Property(adjustment => adjustment.RecordedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(adjustment => adjustment.ProductId);
        builder.HasIndex(adjustment => adjustment.RecordedAtUtc);

        builder.HasOne(adjustment => adjustment.Product)
            .WithMany(product => product.InventoryAdjustments)
            .HasForeignKey(adjustment => adjustment.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(adjustment => adjustment.RecordedByUser)
            .WithMany()
            .HasForeignKey(adjustment => adjustment.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
