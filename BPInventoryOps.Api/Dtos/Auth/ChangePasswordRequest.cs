using System.ComponentModel.DataAnnotations;

namespace BPInventoryOps.Api.Dtos.Auth;

public sealed class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    public string NewPassword { get; init; } = string.Empty;
}
