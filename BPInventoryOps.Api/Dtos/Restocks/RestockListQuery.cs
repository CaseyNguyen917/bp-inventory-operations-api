using System.ComponentModel.DataAnnotations;
using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Restocks;

public sealed class RestockListQuery : PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int? VendorId { get; init; }

    [Range(1, int.MaxValue)]
    public int? ProductId { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}
