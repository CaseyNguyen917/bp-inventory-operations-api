using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Restocks;

public sealed class RestockItemResponse
{
    public required ProductSummaryResponse Product { get; init; }

    public required int QuantityReceived { get; init; }
}
