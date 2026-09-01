namespace BPInventoryOps.Api.Auth;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    string UserId { get; }

    string? Email { get; }

    string? DisplayName { get; }

    IReadOnlyCollection<string> Roles { get; }
}
