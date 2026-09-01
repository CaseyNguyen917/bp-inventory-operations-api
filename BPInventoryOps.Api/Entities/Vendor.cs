namespace BPInventoryOps.Api.Entities;

public class Vendor
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Product> Products { get; set; } = [];

    public ICollection<RestockEvent> RestockEvents { get; set; } = [];
}
