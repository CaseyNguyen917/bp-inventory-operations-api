using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Vendors;

public sealed class CreateVendorRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(120)]
    public string? ContactName { get; init; }

    [MaxLength(30)]
    public string? Phone { get; init; }

    [EmailAddress]
    [MaxLength(256)]
    public string? Email { get; init; }
}
