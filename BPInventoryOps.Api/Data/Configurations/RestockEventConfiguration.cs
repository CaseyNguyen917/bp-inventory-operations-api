using BPInventoryOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BPInventoryOps.Api.Data.Configurations;

public class RestockEventConfiguration : IEntityTypeConfiguration<RestockEvent>
{
    public void Configure(EntityTypeBuilder<RestockEvent> builder)
    {
        builder.ToTable("RestockEvents");

        builder.Property(restock => restock.RecordedByUserId)
            .IsRequired();

        builder.Property(restock => restock.Notes)
            .HasMaxLength(1000);

        builder.Property(restock => restock.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(restock => restock.VendorId);
        builder.HasIndex(restock => restock.ReceivedAtUtc);

        builder.HasOne(restock => restock.Vendor)
            .WithMany(vendor => vendor.RestockEvents)
            .HasForeignKey(restock => restock.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(restock => restock.RecordedByUser)
            .WithMany()
            .HasForeignKey(restock => restock.RecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
