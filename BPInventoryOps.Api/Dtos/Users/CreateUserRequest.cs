using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Users;

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(256)]
    public string InitialPassword { get; init; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; init; } = string.Empty;
}
