using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Products;

public sealed class UpdateProductRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Sku { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Range(1, int.MaxValue)]
    public int PrimaryVendorId { get; init; }

    [Range(0, int.MaxValue)]
    public int ReorderThreshold { get; init; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal Cost { get; init; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal RetailPrice { get; init; }
}
