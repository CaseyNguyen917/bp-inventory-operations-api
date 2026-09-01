using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Users;

public sealed class ChangeUserRoleRequest
{
    [Required]
    [MaxLength(20)]
    public string Role { get; init; } = string.Empty;
}
