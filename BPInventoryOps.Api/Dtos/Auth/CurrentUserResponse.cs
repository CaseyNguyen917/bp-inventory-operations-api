namespace BPInventoryOps.Api.Dtos.Auth;

public sealed class CurrentUserResponse
{
    public required string Id { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }
}
