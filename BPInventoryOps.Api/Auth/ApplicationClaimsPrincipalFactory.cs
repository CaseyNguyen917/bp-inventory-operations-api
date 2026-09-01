using System.Security.Claims;
using BPInventoryOps.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BPInventoryOps.Api.Auth;

public sealed class ApplicationClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(
        userManager,
        roleManager,
        optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(ApplicationClaimTypes.DisplayName, user.DisplayName));

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        }

        return identity;
    }
}
