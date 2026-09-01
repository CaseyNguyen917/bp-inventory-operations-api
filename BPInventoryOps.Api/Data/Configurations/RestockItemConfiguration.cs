using BPInventoryOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BPInventoryOps.Api.Data.Configurations;

public class RestockItemConfiguration : IEntityTypeConfiguration<RestockItem>
{
    public void Configure(EntityTypeBuilder<RestockItem> builder)
    {
        builder.ToTable(
            "RestockItems",
            table => table.HasCheckConstraint(
                "CK_RestockItems_QuantityReceived_Positive",
                "[QuantityReceived] > 0"));

        builder.HasIndex(item => new { item.RestockEventId, item.ProductId })
            .IsUnique();

        builder.HasIndex(item => item.ProductId);

        builder.HasOne(item => item.RestockEvent)
            .WithMany(restock => restock.Items)
            .HasForeignKey(item => item.RestockEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.RestockItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
