using BPInventoryOps.Api.Dtos.Auth;

namespace BPInventoryOps.Api.Services;

public interface IAuthService
{
    Task<CurrentUserResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<CurrentUserResponse> GetCurrentAsync(CancellationToken cancellationToken);

    Task LogoutAsync();

    Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken);
}
