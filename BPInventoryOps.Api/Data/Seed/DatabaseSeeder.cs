using System.Data;
using System.Text.Json;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace BPInventoryOps.Api.Data.Seed;

public sealed class DatabaseSeeder(
    ApplicationDbContext dbContext,
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<SeedDataOptions> options,
    ILogger<DatabaseSeeder> logger)
{
    public const string DemoEmployeeEmail = "employee@bp-inventory.demo";
    public const string DemoManagerEmail = "manager@bp-inventory.demo";
    public const string DemoAdminEmail = "admin@bp-inventory.demo";

    private readonly SeedDataOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        ValidateConfiguredPasswords();

        await EnsureRolesAsync();

        ApplicationUser employee = await EnsureUserAsync(
            DemoEmployeeEmail,
            "Demo Employee",
            _options.DemoEmployeePassword!,
            ApplicationRoles.Employee);
        await EnsureUserAsync(
            DemoManagerEmail,
            "Demo Manager",
            _options.DemoManagerPassword!,
            ApplicationRoles.Manager);
        await EnsureUserAsync(
            DemoAdminEmail,
            "Demo Admin",
            _options.DemoAdminPassword!,
            ApplicationRoles.Admin);

        await SeedBusinessDataAsync(employee.Id, cancellationToken);

        logger.LogInformation(
            "Synthetic demo data initialization completed successfully");
    }

    private async Task EnsureRolesAsync()
    {
        foreach (string roleName in new[]
                 {
                     ApplicationRoles.Employee,
                     ApplicationRoles.Manager,
                     ApplicationRoles.Admin
                 })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                EnsureIdentitySucceeded(
                    await roleManager.CreateAsync(new IdentityRole(roleName)),
                    $"Role '{roleName}' could not be initialized.");
            }
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(
        string email,
        string displayName,
        string configuredPassword,
        string requiredRole)
    {
        ApplicationUser? user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                IsActive = true,
                LockoutEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            EnsureIdentitySucceeded(
                await userManager.CreateAsync(user, configuredPassword),
                $"Demo user '{email}' could not be initialized.");
        }

        IList<string> assignedRoles = await userManager.GetRolesAsync(user);
        string[] otherBusinessRoles = assignedRoles
            .Where(ApplicationRoles.IsBusinessRole)
            .Where(role => !role.Equals(requiredRole, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (otherBusinessRoles.Length > 0)
        {
            EnsureIdentitySucceeded(
                await userManager.RemoveFromRolesAsync(user, otherBusinessRoles),
                $"Demo user '{email}' roles could not be normalized.");
        }

        if (!assignedRoles.Contains(requiredRole, StringComparer.OrdinalIgnoreCase))
        {
            EnsureIdentitySucceeded(
                await userManager.AddToRoleAsync(user, requiredRole),
                $"Demo user '{email}' role could not be initialized.");
        }

        return user;
    }

    private async Task SeedBusinessDataAsync(
        string employeeUserId,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            Dictionary<string, Category> categories = await EnsureCategoriesAsync(
                cancellationToken);
            Dictionary<string, Vendor> vendors = await EnsureVendorsAsync(cancellationToken);
            Dictionary<string, Product> products = await EnsureProductsAsync(
                categories,
                vendors,
                cancellationToken);

            await EnsureRestocksAsync(
                vendors,
                products,
                employeeUserId,
                cancellationToken);
            await EnsureAdjustmentsAsync(
                products,
                employeeUserId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<Dictionary<string, Category>> EnsureCategoriesAsync(
        CancellationToken cancellationToken)
    {
        string[] names =
        [
            "Beverages",
            "Snacks",
            "Candy",
            "Automotive",
            "Household Essentials",
            "Personal Care"
        ];

        Dictionary<string, Category> categories = await dbContext.Categories
            .Where(category => names.Contains(category.Name))
            .ToDictionaryAsync(category => category.Name, cancellationToken);

        DateTime now = DateTime.UtcNow;
        foreach (string name in names)
        {
            if (categories.ContainsKey(name))
            {
                continue;
            }

            Category category = new()
            {
                Name = name,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.Categories.Add(category);
            categories.Add(name, category);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return categories;
    }

    private async Task<Dictionary<string, Vendor>> EnsureVendorsAsync(
        CancellationToken cancellationToken)
    {
        VendorDefinition[] definitions =
        [
            new(
                "Northstar Beverage Supply",
                "Demo Beverage Desk",
                "555-0101",
                "orders@northstar-demo.example"),
            new(
                "Trailside Foods Distribution",
                "Demo Foods Desk",
                "555-0102",
                "orders@trailside-demo.example"),
            new(
                "Meridian General Wholesale",
                "Demo General Desk",
                "555-0103",
                "orders@meridian-demo.example"),
            new(
                "Harborline Auto & Care",
                "Demo Auto Desk",
                "555-0104",
                "orders@harborline-demo.example")
        ];

        string[] names = definitions.Select(definition => definition.Name).ToArray();
        Dictionary<string, Vendor> vendors = await dbContext.Vendors
            .Where(vendor => names.Contains(vendor.Name))
            .ToDictionaryAsync(vendor => vendor.Name, cancellationToken);

        DateTime now = DateTime.UtcNow;
        foreach (VendorDefinition definition in definitions)
        {
            if (vendors.ContainsKey(definition.Name))
            {
                continue;
            }

            Vendor vendor = new()
            {
                Name = definition.Name,
                ContactName = definition.ContactName,
                Phone = definition.Phone,
                Email = definition.Email,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.Vendors.Add(vendor);
            vendors.Add(definition.Name, vendor);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return vendors;
    }

    private async Task<Dictionary<string, Product>> EnsureProductsAsync(
        IReadOnlyDictionary<string, Category> categories,
        IReadOnlyDictionary<string, Vendor> vendors,
        CancellationToken cancellationToken)
    {
        ProductDefinition[] definitions = CreateProductDefinitions();
        string[] skus = definitions.Select(definition => definition.Sku).ToArray();
        Dictionary<string, Product> products = await dbContext.Products
            .Where(product => skus.Contains(product.Sku))
            .ToDictionaryAsync(product => product.Sku, cancellationToken);

        DateTime now = DateTime.UtcNow;
        foreach (ProductDefinition definition in definitions)
        {
            if (products.ContainsKey(definition.Sku))
            {
                continue;
            }

            Product product = new()
            {
                Name = definition.Name,
                Sku = definition.Sku,
                CategoryId = categories[definition.CategoryName].Id,
                PrimaryVendorId = vendors[definition.VendorName].Id,
                QuantityOnHand = 0,
                ReorderThreshold = definition.ReorderThreshold,
                Cost = definition.Cost,
                RetailPrice = definition.RetailPrice,
                IsActive = definition.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.Products.Add(product);
            products.Add(definition.Sku, product);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return products;
    }

    private async Task EnsureRestocksAsync(
        IReadOnlyDictionary<string, Vendor> vendors,
        IReadOnlyDictionary<string, Product> products,
        string employeeUserId,
        CancellationToken cancellationToken)
    {
        RestockDefinition[] definitions = CreateRestockDefinitions();

        foreach (RestockDefinition definition in definitions)
        {
            bool alreadyExists = await dbContext.RestockEvents.AnyAsync(
                restock => restock.Notes == definition.Key,
                cancellationToken);

            if (alreadyExists)
            {
                continue;
            }

            DateTime createdAtUtc = definition.ReceivedAtUtc.AddHours(1);
            RestockEvent restock = new()
            {
                VendorId = vendors[definition.VendorName].Id,
                RecordedByUserId = employeeUserId,
                ReceivedAtUtc = definition.ReceivedAtUtc,
                Notes = definition.Key,
                CreatedAtUtc = createdAtUtc,
                Items = definition.Items.Select(line => new RestockItem
                {
                    ProductId = products[line.Sku].Id,
                    QuantityReceived = line.Quantity
                }).ToList()
            };

            foreach (RestockLineDefinition line in definition.Items)
            {
                Product product = products[line.Sku];
                product.QuantityOnHand = checked(product.QuantityOnHand + line.Quantity);
                product.UpdatedAtUtc = createdAtUtc;
            }

            dbContext.RestockEvents.Add(restock);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = employeeUserId,
                Action = AuditActions.RestockRecorded,
                EntityType = nameof(RestockEvent),
                EntityId = restock.Id.ToString(),
                Details = JsonSerializer.Serialize(new
                {
                    DemoSeedKey = definition.Key,
                    restock.VendorId,
                    ItemCount = definition.Items.Count,
                    TotalUnitsReceived = definition.Items.Sum(item => item.Quantity)
                }),
                TimestampUtc = createdAtUtc
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureAdjustmentsAsync(
        IReadOnlyDictionary<string, Product> products,
        string employeeUserId,
        CancellationToken cancellationToken)
    {
        AdjustmentDefinition[] definitions = CreateAdjustmentDefinitions();

        foreach (AdjustmentDefinition definition in definitions)
        {
            bool alreadyExists = await dbContext.InventoryAdjustments.AnyAsync(
                adjustment => adjustment.Notes == definition.Key,
                cancellationToken);

            if (alreadyExists)
            {
                continue;
            }

            Product product = products[definition.Sku];
            int newQuantity = checked(product.QuantityOnHand + definition.QuantityChange);
            if (newQuantity < 0)
            {
                throw new InvalidOperationException(
                    $"Synthetic adjustment '{definition.Key}' would create negative inventory.");
            }

            int previousQuantity = product.QuantityOnHand;
            product.QuantityOnHand = newQuantity;
            product.UpdatedAtUtc = definition.RecordedAtUtc;

            InventoryAdjustment adjustment = new()
            {
                ProductId = product.Id,
                RecordedByUserId = employeeUserId,
                QuantityChange = definition.QuantityChange,
                Reason = definition.Reason,
                Notes = definition.Key,
                RecordedAtUtc = definition.RecordedAtUtc
            };
            dbContext.InventoryAdjustments.Add(adjustment);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = employeeUserId,
                Action = AuditActions.InventoryAdjusted,
                EntityType = nameof(InventoryAdjustment),
                EntityId = adjustment.Id.ToString(),
                Details = JsonSerializer.Serialize(new
                {
                    DemoSeedKey = definition.Key,
                    adjustment.ProductId,
                    adjustment.QuantityChange,
                    Reason = adjustment.Reason.ToString(),
                    PreviousQuantity = previousQuantity,
                    NewQuantity = newQuantity
                }),
                TimestampUtc = definition.RecordedAtUtc
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private void ValidateConfiguredPasswords()
    {
        List<string> missingKeys = [];

        if (string.IsNullOrWhiteSpace(_options.DemoEmployeePassword))
        {
            missingKeys.Add("SeedData:DemoEmployeePassword");
        }

        if (string.IsNullOrWhiteSpace(_options.DemoManagerPassword))
        {
            missingKeys.Add("SeedData:DemoManagerPassword");
        }

        if (string.IsNullOrWhiteSpace(_options.DemoAdminPassword))
        {
            missingKeys.Add("SeedData:DemoAdminPassword");
        }

        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Demo seeding is enabled, but required configuration is missing: {string.Join(", ", missingKeys)}.");
        }
    }

    private static void EnsureIdentitySucceeded(
        IdentityResult result,
        string message)
    {
        if (!result.Succeeded)
        {
            string errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errors) ? message : $"{message} {errors}");
        }
    }

    private static ProductDefinition[] CreateProductDefinitions()
    {
        const string beverages = "Beverages";
        const string snacks = "Snacks";
        const string candy = "Candy";
        const string automotive = "Automotive";
        const string household = "Household Essentials";
        const string personalCare = "Personal Care";
        const string beverageVendor = "Northstar Beverage Supply";
        const string foodVendor = "Trailside Foods Distribution";
        const string generalVendor = "Meridian General Wholesale";
        const string autoVendor = "Harborline Auto & Care";

        return
        [
            new("DEMO-BEV-001", "Spring Water", beverages, beverageVendor, 12, 0.55m, 1.29m),
            new("DEMO-BEV-002", "Citrus Sparkling Water", beverages, beverageVendor, 8, 0.85m, 1.79m),
            new("DEMO-BEV-003", "Fresh Brewed Iced Tea", beverages, beverageVendor, 10, 1.10m, 2.39m),
            new("DEMO-BEV-004", "Cold Brew Coffee", beverages, beverageVendor, 6, 1.75m, 3.49m),
            new("DEMO-BEV-005", "Berry Sports Drink", beverages, beverageVendor, 8, 1.05m, 2.29m),
            new("DEMO-BEV-006", "Orchard Fruit Juice", beverages, beverageVendor, 6, 1.20m, 2.59m),
            new("DEMO-SNK-001", "Sea Salt Potato Chips", snacks, foodVendor, 10, 0.95m, 2.19m),
            new("DEMO-SNK-002", "Classic Pretzel Twists", snacks, foodVendor, 8, 0.90m, 1.99m),
            new("DEMO-SNK-003", "Cranberry Trail Mix", snacks, foodVendor, 5, 1.60m, 3.29m),
            new("DEMO-SNK-004", "Cheddar Snack Crackers", snacks, foodVendor, 8, 1.05m, 2.39m),
            new("DEMO-CND-001", "Milk Chocolate Bar", candy, foodVendor, 12, 0.70m, 1.69m),
            new("DEMO-CND-002", "Fruit Gummy Pack", candy, foodVendor, 10, 0.75m, 1.79m),
            new("DEMO-CND-003", "Peppermint Tin", candy, foodVendor, 8, 0.80m, 1.89m),
            new("DEMO-CND-004", "Soft Caramel Bites", candy, foodVendor, 10, 0.90m, 1.99m),
            new("DEMO-HOU-001", "Two-Roll Paper Towels", household, generalVendor, 5, 2.40m, 4.99m),
            new("DEMO-HOU-002", "Four-Pack AA Batteries", household, generalVendor, 5, 3.50m, 6.99m),
            new("DEMO-HOU-003", "Small Trash Bags", household, generalVendor, 4, 2.10m, 4.49m),
            new("DEMO-PER-001", "Travel Hand Sanitizer", personalCare, generalVendor, 6, 1.15m, 2.49m),
            new("DEMO-PER-002", "Soft Bristle Toothbrush", personalCare, generalVendor, 5, 1.30m, 2.79m),
            new("DEMO-PER-003", "Pocket Tissue Pack", personalCare, generalVendor, 6, 0.65m, 1.49m),
            new("DEMO-AUT-001", "All-Season Windshield Fluid", automotive, autoVendor, 4, 2.20m, 4.79m),
            new("DEMO-AUT-002", "Quart Synthetic Motor Oil", automotive, autoVendor, 4, 4.90m, 8.99m),
            new("DEMO-AUT-003", "Compact Ice Scraper", automotive, autoVendor, 3, 1.80m, 3.99m),
            new("DEMO-AUT-004", "Fresh Linen Air Freshener", automotive, autoVendor, 4, 0.95m, 2.19m, false)
        ];
    }

    private static RestockDefinition[] CreateRestockDefinitions()
    {
        return
        [
            new(
                "[DEMO-RESTOCK-001]",
                "Northstar Beverage Supply",
                new DateTime(2026, 7, 8, 14, 0, 0, DateTimeKind.Utc),
                [new("DEMO-BEV-001", 30), new("DEMO-BEV-002", 20), new("DEMO-BEV-003", 18)]),
            new(
                "[DEMO-RESTOCK-002]",
                "Northstar Beverage Supply",
                new DateTime(2026, 7, 15, 14, 30, 0, DateTimeKind.Utc),
                [new("DEMO-BEV-004", 12), new("DEMO-BEV-005", 15), new("DEMO-BEV-006", 10)]),
            new(
                "[DEMO-RESTOCK-003]",
                "Trailside Foods Distribution",
                new DateTime(2026, 7, 22, 13, 15, 0, DateTimeKind.Utc),
                [new("DEMO-SNK-001", 20), new("DEMO-SNK-002", 20), new("DEMO-SNK-003", 20), new("DEMO-SNK-004", 20)]),
            new(
                "[DEMO-RESTOCK-004]",
                "Trailside Foods Distribution",
                new DateTime(2026, 7, 29, 13, 45, 0, DateTimeKind.Utc),
                [new("DEMO-CND-001", 30), new("DEMO-CND-002", 30), new("DEMO-CND-003", 30), new("DEMO-CND-004", 30)]),
            new(
                "[DEMO-RESTOCK-005]",
                "Meridian General Wholesale",
                new DateTime(2026, 8, 5, 15, 0, 0, DateTimeKind.Utc),
                [new("DEMO-HOU-001", 12), new("DEMO-HOU-002", 12), new("DEMO-HOU-003", 12)]),
            new(
                "[DEMO-RESTOCK-006]",
                "Meridian General Wholesale",
                new DateTime(2026, 8, 12, 15, 20, 0, DateTimeKind.Utc),
                [new("DEMO-PER-001", 15), new("DEMO-PER-002", 15), new("DEMO-PER-003", 15)]),
            new(
                "[DEMO-RESTOCK-007]",
                "Harborline Auto & Care",
                new DateTime(2026, 8, 19, 16, 0, 0, DateTimeKind.Utc),
                [new("DEMO-AUT-001", 10), new("DEMO-AUT-002", 10), new("DEMO-AUT-003", 10), new("DEMO-AUT-004", 8)]),
            new(
                "[DEMO-RESTOCK-008]",
                "Northstar Beverage Supply",
                new DateTime(2026, 8, 26, 14, 10, 0, DateTimeKind.Utc),
                [new("DEMO-BEV-001", 10), new("DEMO-BEV-003", 8), new("DEMO-BEV-004", 6)])
        ];
    }

    private static AdjustmentDefinition[] CreateAdjustmentDefinitions()
    {
        return
        [
            new("[DEMO-ADJUSTMENT-001]", "DEMO-BEV-001", -28, AdjustmentReason.PhysicalCountCorrection, new DateTime(2026, 8, 27, 10, 0, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-002]", "DEMO-BEV-002", -5, AdjustmentReason.Damage, new DateTime(2026, 8, 27, 10, 15, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-003]", "DEMO-BEV-003", -20, AdjustmentReason.PhysicalCountCorrection, new DateTime(2026, 8, 27, 10, 30, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-004]", "DEMO-BEV-004", -10, AdjustmentReason.Spoilage, new DateTime(2026, 8, 27, 10, 45, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-005]", "DEMO-BEV-005", -8, AdjustmentReason.PhysicalCountCorrection, new DateTime(2026, 8, 27, 11, 0, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-006]", "DEMO-BEV-006", -3, AdjustmentReason.Damage, new DateTime(2026, 8, 27, 11, 15, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-007]", "DEMO-SNK-001", -16, AdjustmentReason.PhysicalCountCorrection, new DateTime(2026, 8, 28, 9, 0, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-008]", "DEMO-SNK-003", -5, AdjustmentReason.Shrinkage, new DateTime(2026, 8, 28, 9, 15, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-009]", "DEMO-HOU-002", -8, AdjustmentReason.PhysicalCountCorrection, new DateTime(2026, 8, 28, 9, 30, 0, DateTimeKind.Utc)),
            new("[DEMO-ADJUSTMENT-010]", "DEMO-AUT-002", -7, AdjustmentReason.Damage, new DateTime(2026, 8, 28, 9, 45, 0, DateTimeKind.Utc))
        ];
    }

    private sealed record VendorDefinition(
        string Name,
        string ContactName,
        string Phone,
        string Email);

    private sealed record ProductDefinition(
        string Sku,
        string Name,
        string CategoryName,
        string VendorName,
        int ReorderThreshold,
        decimal Cost,
        decimal RetailPrice,
        bool IsActive = true);

    private sealed record RestockDefinition(
        string Key,
        string VendorName,
        DateTime ReceivedAtUtc,
        IReadOnlyList<RestockLineDefinition> Items);

    private sealed record RestockLineDefinition(string Sku, int Quantity);

    private sealed record AdjustmentDefinition(
        string Key,
        string Sku,
        int QuantityChange,
        AdjustmentReason Reason,
        DateTime RecordedAtUtc);
}
