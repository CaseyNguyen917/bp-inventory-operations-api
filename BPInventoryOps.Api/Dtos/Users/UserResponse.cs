namespace BPInventoryOps.Api.Dtos.Users;

public sealed class UserResponse
{
    public required string Id { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }

    public required bool IsActive { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
}
