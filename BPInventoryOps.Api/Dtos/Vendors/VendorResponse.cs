namespace BPInventoryOps.Api.Dtos.Vendors;

public sealed class VendorResponse
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? ContactName { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public required bool IsActive { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required DateTime UpdatedAtUtc { get; init; }
}
