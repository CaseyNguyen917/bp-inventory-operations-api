using Microsoft.AspNetCore.Identity;

namespace BPInventoryOps.Api.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
