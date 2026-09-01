using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Restocks;

public sealed class RestockSummaryResponse
{
    public required int Id { get; init; }

    public required VendorSummaryResponse Vendor { get; init; }

    public required DateTime ReceivedAtUtc { get; init; }

    public required int ItemCount { get; init; }

    public required int TotalUnitsReceived { get; init; }
}
