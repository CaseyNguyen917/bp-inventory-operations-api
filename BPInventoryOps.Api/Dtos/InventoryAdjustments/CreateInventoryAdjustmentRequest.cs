using System.ComponentModel.DataAnnotations;
using BPInventoryOps.Api.Enums;

namespace BPInventoryOps.Api.Dtos.InventoryAdjustments;

public sealed class CreateInventoryAdjustmentRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    public int QuantityChange { get; init; }

    [Required]
    public AdjustmentReason? Reason { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}
