using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Products;

public sealed class ProductResponse
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Sku { get; init; }

    public required CategorySummaryResponse Category { get; init; }

    public required VendorSummaryResponse PrimaryVendor { get; init; }

    public required int QuantityOnHand { get; init; }

    public required int ReorderThreshold { get; init; }

    public required decimal Cost { get; init; }

    public required decimal RetailPrice { get; init; }

    public required bool IsLowStock { get; init; }

    public required bool IsActive { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required DateTime UpdatedAtUtc { get; init; }
}
