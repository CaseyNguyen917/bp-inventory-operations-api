using System.ComponentModel.DataAnnotations;
using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.Users;

public sealed class UserListQuery : PaginationQuery
{
    [MaxLength(256)]
    public string? Search { get; init; }

    [MaxLength(20)]
    public string? Role { get; init; }

    public bool IncludeInactive { get; init; }
}
