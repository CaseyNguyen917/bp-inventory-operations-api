namespace BPInventoryOps.Api.Entities;

public class RestockEvent
{
    public int Id { get; set; }

    public int VendorId { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public Vendor Vendor { get; set; } = null!;

    public ApplicationUser RecordedByUser { get; set; } = null!;

    public ICollection<RestockItem> Items { get; set; } = [];
}
