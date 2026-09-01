using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Restocks;

public sealed class CreateRestockRequest
{
    [Range(1, int.MaxValue)]
    public int VendorId { get; init; }

    [Required]
    public DateTime? ReceivedAtUtc { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<RestockItemRequest> Items { get; init; } = [];
}
