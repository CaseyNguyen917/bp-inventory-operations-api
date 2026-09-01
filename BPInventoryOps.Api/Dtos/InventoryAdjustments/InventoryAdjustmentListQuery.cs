using System.ComponentModel.DataAnnotations;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Enums;

namespace BPInventoryOps.Api.Dtos.InventoryAdjustments;

public sealed class InventoryAdjustmentListQuery : PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int? ProductId { get; init; }

    public AdjustmentReason? Reason { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}
