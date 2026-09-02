using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BPInventoryOps.Api.Tests.Infrastructure;

public sealed class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string EmployeeEmail = "employee@integration.test";
    public const string ManagerEmail = "manager@integration.test";
    public const string AdminEmail = "admin@integration.test";
    public const string EmployeePassword = "EmployeeTest1!";
    public const string ManagerPassword = "ManagerTest1!";
    public const string AdminPassword = "AdminTest1!";

    private const string TestDatabasePrefix = "BPInventory_Test_";
    private readonly string _databaseName = $"{TestDatabasePrefix}{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=.\\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connect Timeout=3";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["ConnectionStrings:TestConnection"] = ConnectionString,
                ["SeedData:Enabled"] = "false",
                ["Logging:LogLevel:Default"] = "Warning"
            });
        });
    }

    public async Task InitializeAsync()
    {
        using HttpClient client = CreateClient(CreateClientOptions());
        await ResetDatabaseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DeleteDatabaseAsync();
        await base.DisposeAsync();
    }

    public ApiClientSession CreateSession()
    {
        return new ApiClientSession(CreateClient(CreateClientOptions()));
    }

    public async Task ResetDatabaseAsync()
    {
        EnsureSafeTestDatabase();
        SqlConnection.ClearAllPools();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        UserManager<ApplicationUser> userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        foreach (string role in new[]
                 {
                     ApplicationRoles.Employee,
                     ApplicationRoles.Manager,
                     ApplicationRoles.Admin
                 })
        {
            EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)));
        }

        await CreateUserAsync(
            userManager,
            EmployeeEmail,
            "Integration Employee",
            EmployeePassword,
            ApplicationRoles.Employee);
        await CreateUserAsync(
            userManager,
            ManagerEmail,
            "Integration Manager",
            ManagerPassword,
            ApplicationRoles.Manager);
        await CreateUserAsync(
            userManager,
            AdminEmail,
            "Integration Admin",
            AdminPassword,
            ApplicationRoles.Admin);
    }

    public async Task DeleteDatabaseAsync()
    {
        EnsureSafeTestDatabase();
        SqlConnection.ClearAllPools();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }

    public async Task<T> ExecuteDbAsync<T>(
        Func<ApplicationDbContext, Task<T>> action)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        return await action(dbContext);
    }

    public async Task<string> GetUserIdAsync(string email)
    {
        return await ExecuteDbAsync(async dbContext =>
            await dbContext.Users
                .Where(user => user.Email == email)
                .Select(user => user.Id)
                .SingleAsync());
    }

    private static WebApplicationFactoryClientOptions CreateClientOptions()
    {
        return new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        };
    }

    private void EnsureSafeTestDatabase()
    {
        SqlConnectionStringBuilder builder = new(ConnectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog)
            || !builder.InitialCatalog.StartsWith(
                TestDatabasePrefix,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Destructive test reset refused because the database name is not explicitly test-only.");
        }
    }

    private static async Task CreateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string displayName,
        string password,
        string role)
    {
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

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
