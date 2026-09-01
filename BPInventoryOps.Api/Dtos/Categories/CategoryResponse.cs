namespace BPInventoryOps.Api.Dtos.Categories;

public sealed class CategoryResponse
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required bool IsActive { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    public required DateTime UpdatedAtUtc { get; init; }
}
