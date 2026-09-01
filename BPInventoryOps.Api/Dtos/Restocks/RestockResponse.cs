using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Restocks;

public sealed class RestockResponse
{
    public required int Id { get; init; }

    public required VendorSummaryResponse Vendor { get; init; }

    public required UserSummaryResponse RecordedBy { get; init; }

    public required DateTime ReceivedAtUtc { get; init; }

    public string? Notes { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required IReadOnlyList<RestockItemResponse> Items { get; init; }
}
