namespace BPInventoryOps.Api.Entities;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public int PrimaryVendorId { get; set; }

    public int QuantityOnHand { get; set; }

    public int ReorderThreshold { get; set; }

    public decimal Cost { get; set; }

    public decimal RetailPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Category Category { get; set; } = null!;

    public Vendor PrimaryVendor { get; set; } = null!;

    public ICollection<RestockItem> RestockItems { get; set; } = [];

    public ICollection<InventoryAdjustment> InventoryAdjustments { get; set; } = [];
}
