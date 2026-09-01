using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Common;

public abstract class PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 25;
}
