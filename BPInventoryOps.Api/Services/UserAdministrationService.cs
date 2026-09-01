using System.Data;
using System.Text.Json;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Users;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BPInventoryOps.Api.Services;

public sealed class UserAdministrationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ICurrentUserContext currentUserContext,
    IAuditService auditService,
    ILogger<UserAdministrationService> logger) : IUserAdministrationService
{
    public async Task<PagedResponse<UserResponse>> ListAsync(
        UserListQuery request,
        CancellationToken cancellationToken)
    {
        string? search = NormalizeOptional(request.Search);
        string? roleFilter = null;

        if (NormalizeOptional(request.Role) is string requestedRole)
        {
            roleFilter = NormalizeRole(requestedRole);
        }

        IQueryable<ApplicationUser> query = dbContext.Users.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(user => user.IsActive);
        }

        if (search is not null)
        {
            query = query.Where(user =>
                user.DisplayName.Contains(search)
                || (user.Email != null && user.Email.Contains(search)));
        }

        if (roleFilter is not null)
        {
            string selectedRole = roleFilter;
            query = query.Where(user =>
                (from userRole in dbContext.UserRoles
                 join role in dbContext.Roles on userRole.RoleId equals role.Id
                 where userRole.UserId == user.Id && role.Name == selectedRole
                 select userRole.UserId).Any());
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<UserResponse> items = await query
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(user => new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = (from userRole in dbContext.UserRoles
                        join role in dbContext.Roles on userRole.RoleId equals role.Id
                        where userRole.UserId == user.Id
                            && (role.Name == ApplicationRoles.Employee
                                || role.Name == ApplicationRoles.Manager
                                || role.Name == ApplicationRoles.Admin)
                        orderby role.Name
                        select role.Name).FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<UserResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<UserResponse> GetByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        string normalizedId = NormalizeId(id);

        UserResponse? response = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == normalizedId)
            .Select(user => new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                Role = (from userRole in dbContext.UserRoles
                        join role in dbContext.Roles on userRole.RoleId equals role.Id
                        where userRole.UserId == user.Id
                            && (role.Name == ApplicationRoles.Employee
                                || role.Name == ApplicationRoles.Manager
                                || role.Name == ApplicationRoles.Admin)
                        orderby role.Name
                        select role.Name).FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAtUtc = user.CreatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException("User was not found.");
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string email = request.Email.Trim();
        string displayName = request.DisplayName.Trim();
        string role = NormalizeRole(request.Role);

        if (displayName.Length == 0)
        {
            throw new RequestValidationException("DisplayName is required.");
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            throw new ConflictException($"The '{role}' role has not been initialized.");
        }

        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            LockoutEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        IdentityResult createResult = await userManager.CreateAsync(
            user,
            request.InitialPassword);
        ThrowIfIdentityFailed(createResult, "User creation failed.");

        IdentityResult roleResult = await userManager.AddToRoleAsync(user, role);
        ThrowIfIdentityFailed(roleResult, "User role assignment failed.");

        auditService.Add(
            AuditActions.UserCreated,
            nameof(ApplicationUser),
            user.Id,
            JsonSerializer.Serialize(new { user.Email, user.DisplayName, Role = role }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} was created with role {Role}",
            user.Id,
            role);

        return ToResponse(user, role);
    }

    public async Task<UserResponse> ChangeRoleAsync(
        string id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        string normalizedId = NormalizeId(id);
        string newRole = NormalizeRole(request.Role);

        if (!await roleManager.RoleExistsAsync(newRole))
        {
            throw new ConflictException($"The '{newRole}' role has not been initialized.");
        }

        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        ApplicationUser user = await userManager.FindByIdAsync(normalizedId)
            ?? throw new NotFoundException("User was not found.");

        string[] currentRoles = await GetBusinessRolesAsync(user);

        if (currentRoles.Length == 1
            && currentRoles[0].Equals(newRole, StringComparison.OrdinalIgnoreCase))
        {
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(user, newRole);
        }

        if (user.IsActive
            && currentRoles.Contains(ApplicationRoles.Admin, StringComparer.OrdinalIgnoreCase)
            && !newRole.Equals(ApplicationRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureAnotherActiveAdminExistsAsync(user.Id, cancellationToken);
        }

        if (currentRoles.Length > 0)
        {
            IdentityResult removeResult = await userManager.RemoveFromRolesAsync(
                user,
                currentRoles);
            ThrowIfIdentityFailed(removeResult, "Existing user roles could not be removed.");
        }

        IdentityResult addResult = await userManager.AddToRoleAsync(user, newRole);
        ThrowIfIdentityFailed(addResult, "The new user role could not be assigned.");

        IdentityResult stampResult = await userManager.UpdateSecurityStampAsync(user);
        ThrowIfIdentityFailed(stampResult, "The user's security state could not be updated.");

        auditService.Add(
            AuditActions.UserRoleChanged,
            nameof(ApplicationUser),
            user.Id,
            JsonSerializer.Serialize(new { PreviousRoles = currentRoles, NewRole = newRole }));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} role changed to {Role}",
            user.Id,
            newRole);

        return ToResponse(user, newRole);
    }

    public async Task DeactivateAsync(
        string id,
        CancellationToken cancellationToken)
    {
        string normalizedId = NormalizeId(id);

        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        ApplicationUser user = await userManager.FindByIdAsync(normalizedId)
            ?? throw new NotFoundException("User was not found.");

        if (!user.IsActive)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        if (user.Id == currentUserContext.UserId)
        {
            throw new ConflictException("Users cannot deactivate their own account.");
        }

        string[] currentRoles = await GetBusinessRolesAsync(user);
        if (currentRoles.Contains(ApplicationRoles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            await EnsureAnotherActiveAdminExistsAsync(user.Id, cancellationToken);
        }

        user.IsActive = false;

        IdentityResult stampResult = await userManager.UpdateSecurityStampAsync(user);
        ThrowIfIdentityFailed(stampResult, "The user's security state could not be updated.");

        auditService.Add(
            AuditActions.UserDeactivated,
            nameof(ApplicationUser),
            user.Id,
            null);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("User {UserId} was deactivated", user.Id);
    }

    public async Task<UserResponse> ReactivateAsync(
        string id,
        CancellationToken cancellationToken)
    {
        string normalizedId = NormalizeId(id);

        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        ApplicationUser user = await userManager.FindByIdAsync(normalizedId)
            ?? throw new NotFoundException("User was not found.");

        string[] roles = await GetBusinessRolesAsync(user);
        if (roles.Length != 1)
        {
            throw new ConflictException(
                "The user must have exactly one business role before reactivation.");
        }

        if (user.IsActive)
        {
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(user, roles[0]);
        }

        user.IsActive = true;

        IdentityResult stampResult = await userManager.UpdateSecurityStampAsync(user);
        ThrowIfIdentityFailed(stampResult, "The user's security state could not be updated.");

        auditService.Add(
            AuditActions.UserReactivated,
            nameof(ApplicationUser),
            user.Id,
            null);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("User {UserId} was reactivated", user.Id);

        return ToResponse(user, roles[0]);
    }

    private async Task<string[]> GetBusinessRolesAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        return roles.Where(ApplicationRoles.IsBusinessRole).ToArray();
    }

    private async Task EnsureAnotherActiveAdminExistsAsync(
        string targetUserId,
        CancellationToken cancellationToken)
    {
        int otherActiveAdminCount = await (
            from user in dbContext.Users
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where user.IsActive
                && user.Id != targetUserId
                && role.Name == ApplicationRoles.Admin
            select user.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        if (otherActiveAdminCount == 0)
        {
            throw new ConflictException(
                "The final active Admin cannot be demoted or deactivated.");
        }
    }

    private static string NormalizeRole(string? role)
    {
        if (!ApplicationRoles.TryNormalize(role, out string normalizedRole))
        {
            throw new RequestValidationException(
                "Role must be Employee, Manager, or Admin.");
        }

        return normalizedRole;
    }

    private static string NormalizeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new RequestValidationException("User id is required.");
        }

        return id.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ThrowIfIdentityFailed(
        IdentityResult result,
        string fallbackMessage)
    {
        if (result.Succeeded)
        {
            return;
        }

        IdentityError? duplicateError = result.Errors.FirstOrDefault(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");

        if (duplicateError is not null)
        {
            throw new ConflictException("A user with that email already exists.");
        }

        string detail = string.Join(" ", result.Errors.Select(error => error.Description));
        throw new RequestValidationException(
            string.IsNullOrWhiteSpace(detail) ? fallbackMessage : detail);
    }

    private static UserResponse ToResponse(ApplicationUser user, string role)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}
