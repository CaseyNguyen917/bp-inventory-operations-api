using System.ComponentModel.DataAnnotations;
using BPInventoryOps.Api.Dtos.Restocks;

namespace BPInventoryOps.Api.Pages.Restocks;

// HTML collection binding needs a mutable List; the API DTO stays unchanged.
// The page maps this presentation model explicitly to CreateRestockRequest.
public sealed class RestockForm
{
    [Range(1, int.MaxValue)] public int VendorId { get; set; }
    [Required] public DateTime? ReceivedAtUtc { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    [Required, MinLength(1)] public List<RestockItemRequest> Items { get; set; } = [];
}
