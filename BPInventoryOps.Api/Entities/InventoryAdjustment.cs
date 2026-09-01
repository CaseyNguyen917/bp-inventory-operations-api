using BPInventoryOps.Api.Enums;

namespace BPInventoryOps.Api.Entities;

public class InventoryAdjustment
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;

    public int QuantityChange { get; set; }

    public AdjustmentReason Reason { get; set; }

    public string? Notes { get; set; }

    public DateTime RecordedAtUtc { get; set; }

    public Product Product { get; set; } = null!;

    public ApplicationUser RecordedByUser { get; set; } = null!;
}
