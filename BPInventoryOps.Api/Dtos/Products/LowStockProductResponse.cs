using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Products;

public sealed class LowStockProductResponse
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Sku { get; init; }

    public required int QuantityOnHand { get; init; }

    public required int ReorderThreshold { get; init; }

    public required VendorSummaryResponse PrimaryVendor { get; init; }
}
