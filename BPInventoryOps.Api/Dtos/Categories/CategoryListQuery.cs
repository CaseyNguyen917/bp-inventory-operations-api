using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Categories;

public sealed class CategoryListQuery : PaginationQuery
{
    public string? Search { get; init; }

    public bool IncludeInactive { get; init; }

    public string SortBy { get; init; } = "name";

    public string SortDirection { get; init; } = "asc";
}
