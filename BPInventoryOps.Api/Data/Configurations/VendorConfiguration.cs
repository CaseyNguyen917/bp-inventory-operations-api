using BPInventoryOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BPInventoryOps.Api.Data.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");

        builder.Property(vendor => vendor.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(vendor => vendor.ContactName)
            .HasMaxLength(120);

        builder.Property(vendor => vendor.Phone)
            .HasMaxLength(30);

        builder.Property(vendor => vendor.Email)
            .HasMaxLength(256);

        builder.Property(vendor => vendor.IsActive)
            .HasDefaultValue(true);

        builder.Property(vendor => vendor.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(vendor => vendor.UpdatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(vendor => vendor.Name)
            .IsUnique();
    }
}
