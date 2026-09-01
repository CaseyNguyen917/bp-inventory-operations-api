using BPInventoryOps.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BPInventoryOps.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(auditLog => auditLog.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityId)
            .HasMaxLength(100);

        builder.Property(auditLog => auditLog.Details)
            .HasColumnType("nvarchar(max)");

        builder.Property(auditLog => auditLog.TimestampUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(auditLog => auditLog.TimestampUtc);
        builder.HasIndex(auditLog => auditLog.UserId);
        builder.HasIndex(auditLog => new { auditLog.EntityType, auditLog.EntityId });

        builder.HasOne(auditLog => auditLog.User)
            .WithMany()
            .HasForeignKey(auditLog => auditLog.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
