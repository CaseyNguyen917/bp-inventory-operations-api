using BPInventoryOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BPInventoryOps.Api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.Property(category => category.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(category => category.IsActive)
            .HasDefaultValue(true);

        builder.Property(category => category.CreatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(category => category.UpdatedAtUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(category => category.Name)
            .IsUnique();
    }
}
