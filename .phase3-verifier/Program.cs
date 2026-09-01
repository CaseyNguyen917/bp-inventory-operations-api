using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

const string connectionString =
    "Server=.\\SQLEXPRESS;Database=BPInventoryOps_Phase3Verify;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

ServiceCollection services = new();
services.AddLogging();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 10;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

await using ServiceProvider provider = services.BuildServiceProvider();
await using AsyncServiceScope scope = provider.CreateAsyncScope();

RoleManager<IdentityRole> roleManager = scope.ServiceProvider
    .GetRequiredService<RoleManager<IdentityRole>>();
UserManager<ApplicationUser> userManager = scope.ServiceProvider
    .GetRequiredService<UserManager<ApplicationUser>>();

foreach (string role in new[]
         {
             ApplicationRoles.Employee,
             ApplicationRoles.Manager,
             ApplicationRoles.Admin
         })
{
    if (!await roleManager.RoleExistsAsync(role))
    {
        EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)));
    }
}

await EnsureUserAsync(
    "admin@phase3.test",
    "Verification Admin",
    "VerifyAdmin1!",
    ApplicationRoles.Admin);
await EnsureUserAsync(
    "manager@phase3.test",
    "Verification Manager",
    "VerifyManager1!",
    ApplicationRoles.Manager);
await EnsureUserAsync(
    "employee@phase3.test",
    "Verification Employee",
    "VerifyEmployee1!",
    ApplicationRoles.Employee);

Console.WriteLine("Disposable roles and three verification users are ready.");

async Task EnsureUserAsync(
    string email,
    string displayName,
    string password,
    string role)
{
    ApplicationUser? existing = await userManager.FindByEmailAsync(email);
    if (existing is not null)
    {
        return;
    }

    ApplicationUser user = new()
    {
        UserName = email,
        Email = email,
        DisplayName = displayName,
        IsActive = true,
        LockoutEnabled = true,
        CreatedAtUtc = DateTime.UtcNow
    };

    EnsureSucceeded(await userManager.CreateAsync(user, password));
    EnsureSucceeded(await userManager.AddToRoleAsync(user, role));
}

static void EnsureSucceeded(IdentityResult result)
{
    if (!result.Succeeded)
    {
        throw new InvalidOperationException(
            string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
