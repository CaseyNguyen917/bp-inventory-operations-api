using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Enums;

namespace BPInventoryOps.Api.Dtos.InventoryAdjustments;

public sealed class InventoryAdjustmentResponse
{
    public required int Id { get; init; }

    public required ProductSummaryResponse Product { get; init; }

    public required int QuantityChange { get; init; }

    public required AdjustmentReason Reason { get; init; }

    public string? Notes { get; init; }

    public required UserSummaryResponse RecordedBy { get; init; }

    public required DateTime RecordedAtUtc { get; init; }
}
