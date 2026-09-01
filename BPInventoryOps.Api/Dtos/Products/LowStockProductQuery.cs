using System.ComponentModel.DataAnnotations;
using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Products;

public sealed class LowStockProductQuery : PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public int? VendorId { get; init; }

    public string SortBy { get; init; } = "quantityOnHand";

    public string SortDirection { get; init; } = "asc";
}
