namespace BPInventoryOps.Api.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime TimestampUtc { get; set; }

    public ApplicationUser? User { get; set; }
}
