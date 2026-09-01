using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Auth;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
