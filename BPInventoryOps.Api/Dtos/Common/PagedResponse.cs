namespace BPInventoryOps.Api.Dtos.Common;

public sealed class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }

    public static PagedResponse<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PagedResponse<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
