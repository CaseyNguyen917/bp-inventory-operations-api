using BPInventoryOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BPInventoryOps.Api.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(
            "Products",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Products_QuantityOnHand_NonNegative",
                    "[QuantityOnHand] >= 0");
                table.HasCheckConstraint(
                    "CK_Products_ReorderThreshold_NonNegative",
                    "[ReorderThreshold] >= 0");
                table.HasCheckConstraint(
                    "CK_Products_Cost_NonNegative",
                    "[Cost] >= 0");
                table.HasCheckConstraint(
                    "CK_Products_RetailPrice_NonNegative",
                    "[RetailPrice] >= 0");
            });

        builder.Property(product => product.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(product => product.Sku)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(product => product.QuantityOnHand)
            .HasDefaultValue(0);

        builder.Property(product => product.Cost)
            .HasPrecision(10, 2);

        builder.Property(product => product.RetailPrice)
            .HasPrecision(10, 2);

        builder.Property(product => product.IsActive)
            .HasDefaultValue(true);

        builder.Property(product => product.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(product => product.UpdatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(product => product.Sku)
            .IsUnique();

        builder.HasIndex(product => product.CategoryId);
        builder.HasIndex(product => product.PrimaryVendorId);

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(product => product.PrimaryVendor)
            .WithMany(vendor => vendor.Products)
            .HasForeignKey(product => product.PrimaryVendorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
