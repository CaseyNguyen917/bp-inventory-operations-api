using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Restocks;

public sealed class RestockItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int QuantityReceived { get; init; }
}
