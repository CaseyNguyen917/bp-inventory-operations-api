using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Vendors;

public sealed class VendorListQuery : PaginationQuery
{
    public string? Search { get; init; }

    public bool IncludeInactive { get; init; }

    public string SortBy { get; init; } = "name";

    public string SortDirection { get; init; } = "asc";
}
