using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.Auth;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace BPInventoryOps.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ICurrentUserContext currentUserContext,
    IAuditService auditService,
    ApplicationDbContext dbContext,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<CurrentUserResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string email = request.Email.Trim();
        ApplicationUser? user = await userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        SignInResult result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                logger.LogWarning(
                    "Login rejected because user {UserId} is locked out",
                    user.Id);
            }

            throw new AuthenticationFailedException();
        }

        IReadOnlyList<string> roles = await GetSingleBusinessRoleAsync(user);

        if (roles.Count != 1)
        {
            await signInManager.SignOutAsync();
            throw new AuthenticationFailedException();
        }

        logger.LogInformation("User {UserId} signed in", user.Id);

        return ToCurrentUserResponse(user, roles[0]);
    }

    public async Task<CurrentUserResponse> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user = await userManager.FindByIdAsync(currentUserContext.UserId)
            ?? throw new AuthenticationRequiredException();

        if (!user.IsActive)
        {
            throw new AuthenticationRequiredException();
        }

        IReadOnlyList<string> roles = await GetSingleBusinessRoleAsync(user);
        if (roles.Count != 1)
        {
            throw new ConflictException("The authenticated user must have exactly one business role.");
        }

        return ToCurrentUserResponse(user, roles[0]);
    }

    public async Task LogoutAsync()
    {
        string userId = currentUserContext.UserId;
        await signInManager.SignOutAsync();
        logger.LogInformation("User {UserId} signed out", userId);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user = await userManager.FindByIdAsync(currentUserContext.UserId)
            ?? throw new AuthenticationRequiredException();

        IdentityResult result = await userManager.ChangePasswordAsync(
            user,
            request.CurrentPassword,
            request.NewPassword);

        if (!result.Succeeded)
        {
            string detail = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new RequestValidationException(
                string.IsNullOrWhiteSpace(detail) ? "Password change failed." : detail);
        }

        await signInManager.RefreshSignInAsync(user);

        auditService.Add(
            AuditActions.PasswordChanged,
            nameof(ApplicationUser),
            user.Id,
            null);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} changed their password", user.Id);
    }

    private async Task<IReadOnlyList<string>> GetSingleBusinessRoleAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        return roles
            .Where(ApplicationRoles.IsBusinessRole)
            .ToArray();
    }

    private static CurrentUserResponse ToCurrentUserResponse(
        ApplicationUser user,
        string role)
    {
        return new CurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = role
        };
    }
}
