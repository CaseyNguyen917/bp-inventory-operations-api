using System.Security.Claims;
using BPInventoryOps.Api.Exceptions;

namespace BPInventoryOps.Api.Auth;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new AuthenticationRequiredException();

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? DisplayName => Principal?.FindFirstValue(ApplicationClaimTypes.DisplayName);

    public IReadOnlyCollection<string> Roles => Principal?
        .FindAll(ClaimTypes.Role)
        .Select(claim => claim.Value)
        .ToArray()
        ?? [];
}
