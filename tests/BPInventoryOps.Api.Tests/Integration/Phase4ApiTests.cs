using System.Net;
using System.Text.Json.Nodes;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BPInventoryOps.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public sealed class Phase4ApiTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task HealthEndpoints_WithHealthySql_AreAnonymousAndSafe()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession anonymous = fixture.CreateSession();

        using HttpResponseMessage live = await anonymous.SendAsync(
            HttpMethod.Get,
            "/health");
        using HttpResponseMessage ready = await anonymous.SendAsync(
            HttpMethod.Get,
            "/health/ready");

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);

        string liveBody = await live.Content.ReadAsStringAsync();
        string readyBody = await ready.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", liveBody);
        Assert.Contains("Healthy", readyBody);
        Assert.DoesNotContain("Server=", readyBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQLEXPRESS", readyBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", readyBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductWorkflow_CoversCrudQueriesInactiveFilteringAndLowStockBoundary()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);

        int categoryId = await CreateCategoryAsync(manager, "Test Beverages");
        int vendorId = await CreateVendorAsync(manager, "Test Beverage Vendor");
        JsonNode firstProduct = await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "SQL-PROD-001",
            "Equality Boundary Product",
            reorderThreshold: 0);
        int firstProductId = firstProduct["id"]!.GetValue<int>();

        Assert.Equal(0, firstProduct["quantityOnHand"]!.GetValue<int>());
        Assert.True(firstProduct["isLowStock"]!.GetValue<bool>());

        using HttpResponseMessage duplicate = await manager.SendAsync(
            HttpMethod.Post,
            "/api/products",
            ProductRequest(categoryId, vendorId, "SQL-PROD-001", "Duplicate"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "SQL-PROD-002",
            "Searchable Product",
            reorderThreshold: 4);

        using HttpResponseMessage search = await manager.SendAsync(
            HttpMethod.Get,
            "/api/products?search=SQL-PROD-002");
        JsonNode searchBody = await ReadJsonAsync(search);
        Assert.Equal(1, searchBody["totalCount"]!.GetValue<int>());

        using HttpResponseMessage filteredPage = await manager.SendAsync(
            HttpMethod.Get,
            $"/api/products?categoryId={categoryId}&vendorId={vendorId}&page=1&pageSize=1&sortBy=sku&sortDirection=asc");
        JsonNode pageBody = await ReadJsonAsync(filteredPage);
        Assert.Equal(2, pageBody["totalCount"]!.GetValue<int>());
        Assert.Single(pageBody["items"]!.AsArray());

        using HttpResponseMessage update = await manager.SendAsync(
            HttpMethod.Put,
            $"/api/products/{firstProductId}",
            ProductRequest(
                categoryId,
                vendorId,
                "SQL-PROD-001",
                "Updated Boundary Product",
                reorderThreshold: 0));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        JsonNode updated = await ReadJsonAsync(update);
        Assert.Equal("Updated Boundary Product", updated["name"]!.GetValue<string>());
        Assert.Equal(0, updated["quantityOnHand"]!.GetValue<int>());

        using HttpResponseMessage deactivate = await manager.SendAsync(
            HttpMethod.Delete,
            $"/api/products/{firstProductId}");
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        using HttpResponseMessage activeOnly = await manager.SendAsync(
            HttpMethod.Get,
            "/api/products");
        Assert.Equal(1, (await ReadJsonAsync(activeOnly))["totalCount"]!.GetValue<int>());

        using HttpResponseMessage includingInactive = await manager.SendAsync(
            HttpMethod.Get,
            "/api/products?includeInactive=true");
        Assert.Equal(2, (await ReadJsonAsync(includingInactive))["totalCount"]!.GetValue<int>());

        using HttpResponseMessage reactivate = await manager.SendAsync(
            HttpMethod.Post,
            $"/api/products/{firstProductId}/reactivate",
            new { });
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);

        using HttpResponseMessage lowStock = await manager.SendAsync(
            HttpMethod.Get,
            "/api/products/low-stock");
        JsonArray lowStockItems = (await ReadJsonAsync(lowStock))["items"]!.AsArray();
        Assert.Contains(
            lowStockItems,
            item => item!["id"]!.GetValue<int>() == firstProductId
                && item["quantityOnHand"]!.GetValue<int>()
                == item["reorderThreshold"]!.GetValue<int>());
    }

    [Fact]
    public async Task CategoryAndVendor_WhenReferencedByActiveProduct_EnforceConflictsAndPreserveHistory()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);

        int categoryId = await CreateCategoryAsync(manager, "Original Category");
        int vendorId = await CreateVendorAsync(manager, "Original Vendor");

        using HttpResponseMessage duplicateCategory = await manager.SendAsync(
            HttpMethod.Post,
            "/api/categories",
            new { name = "Original Category" });
        using HttpResponseMessage duplicateVendor = await manager.SendAsync(
            HttpMethod.Post,
            "/api/vendors",
            VendorRequest("Original Vendor"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateVendor.StatusCode);

        using HttpResponseMessage categoryUpdate = await manager.SendAsync(
            HttpMethod.Put,
            $"/api/categories/{categoryId}",
            new { name = "Updated Category" });
        using HttpResponseMessage vendorUpdate = await manager.SendAsync(
            HttpMethod.Put,
            $"/api/vendors/{vendorId}",
            VendorRequest("Updated Vendor"));
        Assert.Equal(HttpStatusCode.OK, categoryUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, vendorUpdate.StatusCode);

        JsonNode product = await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "MASTER-001",
            "Master Reference Product",
            3);
        int productId = product["id"]!.GetValue<int>();

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await manager.SendAsync(HttpMethod.Delete, $"/api/categories/{categoryId}"))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await manager.SendAsync(HttpMethod.Delete, $"/api/vendors/{vendorId}"))
                .StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(HttpMethod.Delete, $"/api/products/{productId}"))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(HttpMethod.Delete, $"/api/categories/{categoryId}"))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(HttpMethod.Delete, $"/api/vendors/{vendorId}"))
                .StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await manager.SendAsync(
                HttpMethod.Post,
                $"/api/categories/{categoryId}/reactivate",
                new { })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await manager.SendAsync(
                HttpMethod.Post,
                $"/api/vendors/{vendorId}/reactivate",
                new { })).StatusCode);

        using HttpResponseMessage historicalProduct = await manager.SendAsync(
            HttpMethod.Get,
            $"/api/products/{productId}");
        JsonNode historicalBody = await ReadJsonAsync(historicalProduct);
        Assert.False(historicalBody["isActive"]!.GetValue<bool>());
    }

    [Fact]
    public async Task RecordRestock_WithOneAndMultipleValidItems_UpdatesQuantitiesAndActor()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);
        using ApiClientSession employee = await LoginAsync(
            ApiTestFixture.EmployeeEmail,
            ApiTestFixture.EmployeePassword);

        int categoryId = await CreateCategoryAsync(manager, "Restock Category");
        int vendorId = await CreateVendorAsync(manager, "Restock Vendor");
        int firstId = (await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "RESTOCK-001",
            "First Restock Product",
            2))["id"]!.GetValue<int>();
        int secondId = (await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "RESTOCK-002",
            "Second Restock Product",
            2))["id"]!.GetValue<int>();

        using HttpResponseMessage oneLine = await employee.SendAsync(
            HttpMethod.Post,
            "/api/restocks",
            RestockRequest(vendorId, (firstId, 3)));
        Assert.Equal(HttpStatusCode.Created, oneLine.StatusCode);
        JsonNode oneLineBody = await ReadJsonAsync(oneLine);
        Assert.Equal(
            await fixture.GetUserIdAsync(ApiTestFixture.EmployeeEmail),
            oneLineBody["recordedBy"]!["id"]!.GetValue<string>());

        using HttpResponseMessage multipleLines = await employee.SendAsync(
            HttpMethod.Post,
            "/api/restocks",
            RestockRequest(vendorId, (firstId, 2), (secondId, 4)));
        Assert.Equal(HttpStatusCode.Created, multipleLines.StatusCode);

        Assert.Equal(5, await GetProductQuantityAsync(employee, firstId));
        Assert.Equal(4, await GetProductQuantityAsync(employee, secondId));

        using HttpResponseMessage list = await employee.SendAsync(
            HttpMethod.Get,
            "/api/restocks");
        Assert.Equal(2, (await ReadJsonAsync(list))["totalCount"]!.GetValue<int>());
    }

    [Fact]
    public async Task RecordRestock_WhenAnyInputIsInvalid_PersistsNoPartialState()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);
        using ApiClientSession employee = await LoginAsync(
            ApiTestFixture.EmployeeEmail,
            ApiTestFixture.EmployeePassword);

        int categoryId = await CreateCategoryAsync(manager, "Rollback Category");
        int vendorId = await CreateVendorAsync(manager, "Primary Restock Vendor");
        int otherVendorId = await CreateVendorAsync(manager, "Other Restock Vendor");
        int inactiveVendorId = await CreateVendorAsync(manager, "Inactive Restock Vendor");
        int productId = (await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "ROLLBACK-001",
            "Rollback Product",
            1))["id"]!.GetValue<int>();

        using HttpResponseMessage invalidLine = await employee.SendAsync(
            HttpMethod.Post,
            "/api/restocks",
            RestockRequest(vendorId, (productId, 3), (999999, 2)));
        Assert.Equal(HttpStatusCode.NotFound, invalidLine.StatusCode);
        Assert.Equal(0, await GetProductQuantityAsync(employee, productId));
        Assert.Equal(
            0,
            await fixture.ExecuteDbAsync(db => db.RestockEvents.CountAsync()));

        Assert.Equal(
            HttpStatusCode.NotFound,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(999999, (productId, 1)))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(otherVendorId, (productId, 1)))).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(vendorId, (productId, 1), (productId, 2)))).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(vendorId, (productId, 0)))).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(
                HttpMethod.Delete,
                $"/api/vendors/{inactiveVendorId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(inactiveVendorId, (productId, 1)))).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(HttpMethod.Delete, $"/api/products/{productId}"))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(vendorId, (productId, 1)))).StatusCode);
    }

    [Fact]
    public async Task RecordAdjustment_WithValidAndInvalidChanges_PreservesInventoryIntegrity()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);
        using ApiClientSession employee = await LoginAsync(
            ApiTestFixture.EmployeeEmail,
            ApiTestFixture.EmployeePassword);

        int categoryId = await CreateCategoryAsync(manager, "Adjustment Category");
        int vendorId = await CreateVendorAsync(manager, "Adjustment Vendor");
        int productId = (await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "ADJUST-001",
            "Adjustment Product",
            2))["id"]!.GetValue<int>();

        Assert.Equal(
            HttpStatusCode.Created,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(vendorId, (productId, 5)))).StatusCode);

        using HttpResponseMessage positive = await employee.SendAsync(
            HttpMethod.Post,
            "/api/inventory-adjustments",
            AdjustmentRequest(productId, 2, "ManualCorrection"));
        using HttpResponseMessage negative = await employee.SendAsync(
            HttpMethod.Post,
            "/api/inventory-adjustments",
            AdjustmentRequest(productId, -3, "Damage"));
        Assert.Equal(HttpStatusCode.Created, positive.StatusCode);
        Assert.Equal(HttpStatusCode.Created, negative.StatusCode);
        Assert.Equal(4, await GetProductQuantityAsync(employee, productId));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/inventory-adjustments",
                AdjustmentRequest(productId, 0, "Other"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/inventory-adjustments",
                AdjustmentRequest(productId, -5, "Damage"))).StatusCode);
        Assert.Equal(4, await GetProductQuantityAsync(employee, productId));
        Assert.Equal(
            2,
            await fixture.ExecuteDbAsync(db => db.InventoryAdjustments.CountAsync()));

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(HttpMethod.Delete, $"/api/products/{productId}"))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/inventory-adjustments",
                AdjustmentRequest(productId, 1, "Other"))).StatusCode);
    }

    [Fact]
    public async Task Authentication_CoversLoginMePasswordChangeLogoutInactiveAndLockout()
    {
        await fixture.ResetDatabaseAsync();

        using ApiClientSession invalid = fixture.CreateSession();
        using HttpResponseMessage invalidPassword = await invalid.LoginAsync(
            ApiTestFixture.ManagerEmail,
            "IncorrectPassword1!");
        using ApiClientSession missing = fixture.CreateSession();
        using HttpResponseMessage missingUser = await missing.LoginAsync(
            "missing@integration.test",
            "IncorrectPassword1!");
        Assert.Equal(HttpStatusCode.Unauthorized, invalidPassword.StatusCode);
        JsonNode invalidPasswordProblem = await ReadJsonAsync(invalidPassword);
        JsonNode missingUserProblem = await ReadJsonAsync(missingUser);
        Assert.Equal(
            invalidPasswordProblem["status"]!.GetValue<int>(),
            missingUserProblem["status"]!.GetValue<int>());
        Assert.Equal(
            invalidPasswordProblem["title"]!.GetValue<string>(),
            missingUserProblem["title"]!.GetValue<string>());
        Assert.Equal(
            invalidPasswordProblem["detail"]!.GetValue<string>(),
            missingUserProblem["detail"]!.GetValue<string>());
        Assert.False(string.IsNullOrWhiteSpace(
            invalidPasswordProblem["traceId"]!.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(
            missingUserProblem["traceId"]!.GetValue<string>()));

        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);
        using HttpResponseMessage me = await manager.SendAsync(
            HttpMethod.Get,
            "/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(
            ApplicationRoles.Manager,
            (await ReadJsonAsync(me))["role"]!.GetValue<string>());

        const string changedPassword = "ManagerChanged1!";
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(
                HttpMethod.Post,
                "/api/auth/change-password",
                new
                {
                    currentPassword = ApiTestFixture.ManagerPassword,
                    newPassword = changedPassword
                })).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await manager.SendAsync(HttpMethod.Post, "/api/auth/logout", new { }))
                .StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await manager.SendAsync(HttpMethod.Get, "/api/auth/me")).StatusCode);

        using ApiClientSession oldPassword = fixture.CreateSession();
        using ApiClientSession newPassword = fixture.CreateSession();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await oldPassword.LoginAsync(
                ApiTestFixture.ManagerEmail,
                ApiTestFixture.ManagerPassword)).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await newPassword.LoginAsync(
                ApiTestFixture.ManagerEmail,
                changedPassword)).StatusCode);

        using ApiClientSession admin = await LoginAsync(
            ApiTestFixture.AdminEmail,
            ApiTestFixture.AdminPassword);
        string employeeId = await fixture.GetUserIdAsync(ApiTestFixture.EmployeeEmail);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await admin.SendAsync(
                HttpMethod.Post,
                $"/api/users/{employeeId}/deactivate",
                new { })).StatusCode);
        using ApiClientSession inactive = fixture.CreateSession();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await inactive.LoginAsync(
                ApiTestFixture.EmployeeEmail,
                ApiTestFixture.EmployeePassword)).StatusCode);

        await fixture.ResetDatabaseAsync();
        using ApiClientSession lockout = fixture.CreateSession();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            using HttpResponseMessage failed = await lockout.LoginAsync(
                ApiTestFixture.EmployeeEmail,
                "IncorrectPassword1!");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        using HttpResponseMessage lockedCorrect = await lockout.LoginAsync(
            ApiTestFixture.EmployeeEmail,
            ApiTestFixture.EmployeePassword);
        Assert.Equal(HttpStatusCode.Unauthorized, lockedCorrect.StatusCode);
    }

    [Fact]
    public async Task AuthorizationAndAntiforgery_EnforceTheEndpointMatrix()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession anonymous = fixture.CreateSession();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.SendAsync(HttpMethod.Get, "/api/products")).StatusCode);

        using ApiClientSession employee = await LoginAsync(
            ApiTestFixture.EmployeeEmail,
            ApiTestFixture.EmployeePassword);
        Assert.Equal(
            HttpStatusCode.OK,
            (await employee.SendAsync(HttpMethod.Get, "/api/products")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/categories",
                new { name = "Forbidden Category" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await employee.SendAsync(HttpMethod.Get, "/api/audit-logs")).StatusCode);

        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await manager.SendAsync(
                HttpMethod.Post,
                "/api/categories",
                new { name = "Missing CSRF Category" },
                includeAntiforgery: false)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Created,
            (await manager.SendAsync(
                HttpMethod.Post,
                "/api/categories",
                new { name = "Allowed Category" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await manager.SendAsync(HttpMethod.Get, "/api/users")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await manager.SendAsync(HttpMethod.Get, "/api/audit-logs")).StatusCode);

        using ApiClientSession admin = await LoginAsync(
            ApiTestFixture.AdminEmail,
            ApiTestFixture.AdminPassword);
        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.SendAsync(HttpMethod.Get, "/api/users")).StatusCode);
    }

    [Fact]
    public async Task UserAdministration_CoversCreationRolesDuplicatesAndInactiveLogin()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession admin = await LoginAsync(
            ApiTestFixture.AdminEmail,
            ApiTestFixture.AdminPassword);

        var createRequest = new
        {
            email = "created@integration.test",
            displayName = "Created Integration User",
            initialPassword = "CreatedUser1!",
            role = ApplicationRoles.Employee
        };
        using HttpResponseMessage created = await admin.SendAsync(
            HttpMethod.Post,
            "/api/users",
            createRequest);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        JsonNode createdBody = await ReadJsonAsync(created);
        string userId = createdBody["id"]!.GetValue<string>();

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await admin.SendAsync(HttpMethod.Post, "/api/users", createRequest)).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await admin.SendAsync(
                HttpMethod.Post,
                "/api/users",
                new
                {
                    email = "invalid-role@integration.test",
                    displayName = "Invalid Role",
                    initialPassword = "InvalidRole1!",
                    role = "Owner"
                })).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.SendAsync(
                HttpMethod.Put,
                $"/api/users/{userId}/role",
                new { role = ApplicationRoles.Manager })).StatusCode);

        string[] roles = await fixture.ExecuteDbAsync(async db =>
            await (from userRole in db.UserRoles
                   join role in db.Roles on userRole.RoleId equals role.Id
                   where userRole.UserId == userId
                   select role.Name!).ToArrayAsync());
        Assert.Equal([ApplicationRoles.Manager], roles);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await admin.SendAsync(
                HttpMethod.Post,
                $"/api/users/{userId}/deactivate",
                new { })).StatusCode);
        using ApiClientSession inactive = fixture.CreateSession();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await inactive.LoginAsync(
                "created@integration.test",
                "CreatedUser1!")).StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.SendAsync(
                HttpMethod.Post,
                $"/api/users/{userId}/reactivate",
                new { })).StatusCode);
        using ApiClientSession reactivated = fixture.CreateSession();
        JsonNode loginBody = await ReadJsonAsync(
            await reactivated.LoginAsync(
                "created@integration.test",
                "CreatedUser1!"));
        Assert.Equal(ApplicationRoles.Manager, loginBody["role"]!.GetValue<string>());
    }

    [Fact]
    public async Task UserAdministration_ProtectsFinalAdminAndHistoricalUserReferences()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession admin = await LoginAsync(
            ApiTestFixture.AdminEmail,
            ApiTestFixture.AdminPassword);
        string adminId = await fixture.GetUserIdAsync(ApiTestFixture.AdminEmail);

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await admin.SendAsync(
                HttpMethod.Post,
                $"/api/users/{adminId}/deactivate",
                new { })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await admin.SendAsync(
                HttpMethod.Put,
                $"/api/users/{adminId}/role",
                new { role = ApplicationRoles.Manager })).StatusCode);

        using HttpResponseMessage secondAdmin = await admin.SendAsync(
            HttpMethod.Post,
            "/api/users",
            new
            {
                email = "second-admin@integration.test",
                displayName = "Second Admin",
                initialPassword = "SecondAdmin1!",
                role = ApplicationRoles.Admin
            });
        string secondAdminId = (await ReadJsonAsync(secondAdmin))["id"]!.GetValue<string>();
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await admin.SendAsync(
                HttpMethod.Post,
                $"/api/users/{secondAdminId}/deactivate",
                new { })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await admin.SendAsync(
                HttpMethod.Put,
                $"/api/users/{adminId}/role",
                new { role = ApplicationRoles.Manager })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.SendAsync(
                HttpMethod.Post,
                $"/api/users/{secondAdminId}/reactivate",
                new { })).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await admin.SendAsync(
                HttpMethod.Put,
                $"/api/users/{secondAdminId}/role",
                new { role = ApplicationRoles.Manager })).StatusCode);

        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);
        using ApiClientSession employee = await LoginAsync(
            ApiTestFixture.EmployeeEmail,
            ApiTestFixture.EmployeePassword);
        int categoryId = await CreateCategoryAsync(manager, "History Category");
        int vendorId = await CreateVendorAsync(manager, "History Vendor");
        int productId = (await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "HISTORY-001",
            "History Product",
            2))["id"]!.GetValue<int>();
        JsonNode restock = await ReadJsonAsync(
            await employee.SendAsync(
                HttpMethod.Post,
                "/api/restocks",
                RestockRequest(vendorId, (productId, 2))));
        int restockId = restock["id"]!.GetValue<int>();
        string employeeId = await fixture.GetUserIdAsync(ApiTestFixture.EmployeeEmail);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await admin.SendAsync(
                HttpMethod.Post,
                $"/api/users/{employeeId}/deactivate",
                new { })).StatusCode);
        JsonNode historicalRestock = await ReadJsonAsync(
            await manager.SendAsync(HttpMethod.Get, $"/api/restocks/{restockId}"));
        Assert.Equal(
            employeeId,
            historicalRestock["recordedBy"]!["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task ProblemDetails_CoversExpectedStatusesAndSafeUnexpectedFailure()
    {
        await fixture.ResetDatabaseAsync();
        using ApiClientSession manager = await LoginAsync(
            ApiTestFixture.ManagerEmail,
            ApiTestFixture.ManagerPassword);
        using ApiClientSession employee = await LoginAsync(
            ApiTestFixture.EmployeeEmail,
            ApiTestFixture.EmployeePassword);
        using ApiClientSession anonymous = fixture.CreateSession();

        using HttpResponseMessage validation = await manager.SendAsync(
            HttpMethod.Post,
            "/api/products",
            new { });
        Assert.Equal(HttpStatusCode.BadRequest, validation.StatusCode);
        Assert.Equal(
            "application/problem+json",
            validation.Content.Headers.ContentType?.MediaType);

        using HttpResponseMessage notFound = await manager.SendAsync(
            HttpMethod.Get,
            "/api/products/999999");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);

        int categoryId = await CreateCategoryAsync(manager, "Problem Category");
        int vendorId = await CreateVendorAsync(manager, "Problem Vendor");
        await CreateProductAsync(
            manager,
            categoryId,
            vendorId,
            "PROBLEM-001",
            "Problem Product",
            1);
        using HttpResponseMessage conflict = await manager.SendAsync(
            HttpMethod.Post,
            "/api/products",
            ProductRequest(categoryId, vendorId, "PROBLEM-001", "Duplicate"));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await anonymous.SendAsync(HttpMethod.Get, "/api/products")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await employee.SendAsync(
                HttpMethod.Post,
                "/api/categories",
                new { name = "Forbidden" })).StatusCode);

        await fixture.DeleteDatabaseAsync();

        using HttpResponseMessage unexpected = await manager.SendAsync(
            HttpMethod.Get,
            "/api/products");
        Assert.Equal(HttpStatusCode.InternalServerError, unexpected.StatusCode);
        string unexpectedBody = await unexpected.Content.ReadAsStringAsync();
        Assert.Contains("An unexpected error occurred", unexpectedBody);
        Assert.DoesNotContain("SqlException", unexpectedBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQLEXPRESS", unexpectedBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Server=", unexpectedBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack", unexpectedBody, StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage live = await anonymous.SendAsync(HttpMethod.Get, "/health");
        using HttpResponseMessage ready = await anonymous.SendAsync(
            HttpMethod.Get,
            "/health/ready");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        string readyBody = await ready.Content.ReadAsStringAsync();
        Assert.Contains("Unhealthy", readyBody);
        Assert.DoesNotContain("Exception", readyBody, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ApiClientSession> LoginAsync(string email, string password)
    {
        ApiClientSession session = fixture.CreateSession();
        HttpResponseMessage response = await session.LoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Dispose();
        return session;
    }

    private static async Task<int> CreateCategoryAsync(
        ApiClientSession session,
        string name)
    {
        using HttpResponseMessage response = await session.SendAsync(
            HttpMethod.Post,
            "/api/categories",
            new { name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response))["id"]!.GetValue<int>();
    }

    private static async Task<int> CreateVendorAsync(
        ApiClientSession session,
        string name)
    {
        using HttpResponseMessage response = await session.SendAsync(
            HttpMethod.Post,
            "/api/vendors",
            VendorRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response))["id"]!.GetValue<int>();
    }

    private static async Task<JsonNode> CreateProductAsync(
        ApiClientSession session,
        int categoryId,
        int vendorId,
        string sku,
        string name,
        int reorderThreshold)
    {
        using HttpResponseMessage response = await session.SendAsync(
            HttpMethod.Post,
            "/api/products",
            ProductRequest(categoryId, vendorId, sku, name, reorderThreshold));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private static async Task<int> GetProductQuantityAsync(
        ApiClientSession session,
        int productId)
    {
        using HttpResponseMessage response = await session.SendAsync(
            HttpMethod.Get,
            $"/api/products/{productId}");
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response))["quantityOnHand"]!.GetValue<int>();
    }

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)
            ?? throw new InvalidOperationException("Expected a JSON response body.");
    }

    private static object ProductRequest(
        int categoryId,
        int primaryVendorId,
        string sku,
        string name,
        int reorderThreshold = 2)
    {
        return new
        {
            name,
            sku,
            categoryId,
            primaryVendorId,
            reorderThreshold,
            cost = 1.25m,
            retailPrice = 2.75m
        };
    }

    private static object VendorRequest(string name)
    {
        return new
        {
            name,
            contactName = "Synthetic Test Contact",
            phone = "555-0199",
            email = "vendor@integration.example"
        };
    }

    private static object RestockRequest(
        int vendorId,
        params (int ProductId, int Quantity)[] lines)
    {
        return new
        {
            vendorId,
            receivedAtUtc = new DateTime(2026, 9, 1, 16, 0, 0, DateTimeKind.Utc),
            notes = "SQL integration test restock",
            items = lines.Select(line => new
            {
                productId = line.ProductId,
                quantityReceived = line.Quantity
            })
        };
    }

    private static object AdjustmentRequest(
        int productId,
        int quantityChange,
        string reason)
    {
        return new
        {
            productId,
            quantityChange,
            reason,
            notes = "SQL integration test adjustment"
        };
    }
}
