using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Users;

namespace BPInventoryOps.Api.Services;

public interface IUserAdministrationService
{
    Task<PagedResponse<UserResponse>> ListAsync(
        UserListQuery request,
        CancellationToken cancellationToken);

    Task<UserResponse> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);

    Task<UserResponse> ChangeRoleAsync(
        string id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken);

    Task DeactivateAsync(string id, CancellationToken cancellationToken);

    Task<UserResponse> ReactivateAsync(string id, CancellationToken cancellationToken);
}
