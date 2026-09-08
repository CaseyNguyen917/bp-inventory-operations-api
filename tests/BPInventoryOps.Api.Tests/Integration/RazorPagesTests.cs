using System.Net;
using System.Text.RegularExpressions;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BPInventoryOps.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public sealed class RazorPagesTests(ApiTestFixture fixture)
{
    [Fact]
    public async Task Login_UsesFormCsrf_LocalRedirects_AndDoesNotChangeApiChallenges()
    {
        await fixture.ResetDatabaseAsync();
        using var browser = Browser();
        using var page = await browser.GetAsync("/Products");
        Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
        Assert.Contains("/Account/Login", page.Headers.Location!.ToString());
        using var api = await browser.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
        Assert.Equal("application/problem+json", api.Content.Headers.ContentType!.MediaType);

        using var missingToken = await browser.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = ApiTestFixture.EmployeeEmail,
            ["Input.Password"] = ApiTestFixture.EmployeePassword
        }));
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        using var login = await PostAsync(browser, "/Account/Login", new()
        {
            ["Input.Email"] = ApiTestFixture.EmployeeEmail,
            ["Input.Password"] = ApiTestFixture.EmployeePassword,
            ["ReturnUrl"] = "https://untrusted.example/"
        });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location!.ToString());
        using var dashboard = await browser.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.True(dashboard.Headers.CacheControl!.NoStore);
        Assert.Equal("DENY", Assert.Single(dashboard.Headers.GetValues("X-Frame-Options")));
        Assert.DoesNotContain("Team access", await dashboard.Content.ReadAsStringAsync());
        using var logout = await PostAsync(browser, "/Account/Logout", new());
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        using var after = await browser.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Employee_CanReadWorkspace_ButCannotForgeManagerOrAdminPagePosts()
    {
        await fixture.ResetDatabaseAsync();
        using var browser = await LoginAsync();
        foreach (var path in new[] { "/", "/Products", "/LowStock", "/Categories", "/Vendors", "/Restocks", "/Restocks/Create", "/Adjustments", "/Adjustments/Create", "/Account/Password" })
        {
            using var page = await browser.GetAsync(path);
            Assert.True(page.IsSuccessStatusCode, $"{path}: {page.StatusCode} {page.Headers} {await page.Content.ReadAsStringAsync()}");
        }
        foreach (var path in new[] { "/Products/Edit", "/Categories/Edit", "/Vendors/Edit", "/Audit", "/Users", "/Users/Create" })
        {
            using var page = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
            Assert.Contains("/Account/AccessDenied", page.Headers.Location!.ToString());
            using var forged = await PostAsync(browser, path, new() { ["Input.Name"] = "Forbidden" }, "/Account/Logout");
            Assert.Equal(HttpStatusCode.Redirect, forged.StatusCode);
            Assert.Contains("/Account/AccessDenied", forged.Headers.Location!.ToString());
        }
        using var denied = await browser.GetAsync("/Account/AccessDenied");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var api = await browser.GetAsync("/api/audit-logs");
        Assert.Equal(HttpStatusCode.Forbidden, api.StatusCode);
        Assert.Equal(0, await fixture.ExecuteDbAsync(db => db.Products.CountAsync()));
    }

    [Fact]
    public async Task CatalogForms_PreserveQuantities_EncodeNames_AndEnforceDeactivationRules()
    {
        await fixture.ResetDatabaseAsync();
        using var browser = await LoginAsync(manager: true);
        using var category = await PostAsync(browser, "/Categories/Edit", new() { ["Input.Name"] = "Browser category" });
        Assert.Equal(HttpStatusCode.Redirect, category.StatusCode);
        using var vendor = await PostAsync(browser, "/Vendors/Edit", new() { ["Input.Name"] = "Browser vendor", ["Input.Email"] = "vendor@example.test" });
        Assert.Equal(HttpStatusCode.Redirect, vendor.StatusCode);
        int categoryId = await fixture.ExecuteDbAsync(db => db.Categories.Select(x => x.Id).SingleAsync());
        int vendorId = await fixture.ExecuteDbAsync(db => db.Vendors.Select(x => x.Id).SingleAsync());
        var fields = ProductFields(categoryId, vendorId);
        fields["Input.QuantityOnHand"] = "999";
        using var created = await PostAsync(browser, "/Products/Edit", fields);
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var product = await fixture.ExecuteDbAsync(db => db.Products.AsNoTracking().SingleAsync());
        Assert.Equal(0, product.QuantityOnHand);
        fields["Input.Name"] = "<script>alert('test')</script>";
        using var update = await PostAsync(browser, $"/Products/Edit/{product.Id}", fields);
        Assert.Equal(HttpStatusCode.Redirect, update.StatusCode);
        Assert.Equal(0, await fixture.ExecuteDbAsync(db => db.Products.Select(x => x.QuantityOnHand).SingleAsync()));
        using var details = await browser.GetAsync($"/Products/Details/{product.Id}");
        string html = await details.Content.ReadAsStringAsync();
        Assert.Contains("&lt;script&gt;", html);
        Assert.DoesNotContain("<script>alert", html);
        using var blockedCategory = await PostAsync(browser, $"/Categories/Edit/{categoryId}?handler=Deactivate", new());
        Assert.Equal(HttpStatusCode.Conflict, blockedCategory.StatusCode);
        using var blockedVendor = await PostAsync(browser, $"/Vendors/Edit/{vendorId}?handler=Deactivate", new());
        Assert.Equal(HttpStatusCode.Conflict, blockedVendor.StatusCode);
        using var deactivate = await PostAsync(browser, $"/Products/Edit/{product.Id}?handler=Deactivate", new());
        Assert.Equal(HttpStatusCode.Redirect, deactivate.StatusCode);
        using var reactivate = await PostAsync(browser, $"/Products/Edit/{product.Id}?handler=Reactivate", new());
        Assert.Equal(HttpStatusCode.Redirect, reactivate.StatusCode);
        Assert.True(await fixture.ExecuteDbAsync(db => db.Products.Select(x => x.IsActive).SingleAsync()));
        Assert.True(await fixture.ExecuteDbAsync(db => db.AuditLogs.AnyAsync(x => x.Action == "ProductUpdated")));
    }

    [Fact]
    public async Task RestockForm_BindsMultipleLines_AndRejectsDuplicatesWithoutPartialWrites()
    {
        await fixture.ResetDatabaseAsync();
        var (vendor, first, second) = await SeedCatalogAsync();
        using var browser = await LoginAsync();
        var fields = new Dictionary<string, string>
        {
            ["Input.VendorId"] = vendor.ToString(),
            ["Input.ReceivedAtUtc"] = "2026-09-01T12:30",
            ["Input.Items[0].ProductId"] = first.ToString(),
            ["Input.Items[0].QuantityReceived"] = "3",
            ["Input.Items[1].ProductId"] = second.ToString(),
            ["Input.Items[1].QuantityReceived"] = "4",
            ["Input.Notes"] = "Browser delivery"
        };
        using var saved = await PostAsync(browser, "/Restocks/Create", fields, $"/Restocks/Create?vendorId={vendor}");
        Assert.True(saved.StatusCode == HttpStatusCode.Redirect, await saved.Content.ReadAsStringAsync());
        using var details = await browser.GetAsync(saved.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Contains("Browser delivery", await details.Content.ReadAsStringAsync());
        Assert.Equal(new[] { 13, 14 }, await fixture.ExecuteDbAsync(db => db.Products.OrderBy(x => x.Id).Select(x => x.QuantityOnHand).ToArrayAsync()));
        fields["Input.Items[1].ProductId"] = first.ToString();
        using var duplicate = await PostAsync(browser, "/Restocks/Create", fields, $"/Restocks/Create?vendorId={vendor}");
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Contains("Browser delivery", await duplicate.Content.ReadAsStringAsync());
        Assert.Equal(1, await fixture.ExecuteDbAsync(db => db.RestockEvents.CountAsync()));
        Assert.Equal(2, await fixture.ExecuteDbAsync(db => db.RestockItems.CountAsync()));
        Assert.Equal(new[] { 13, 14 }, await fixture.ExecuteDbAsync(db => db.Products.OrderBy(x => x.Id).Select(x => x.QuantityOnHand).ToArrayAsync()));
        Assert.Equal(1, await fixture.ExecuteDbAsync(db => db.AuditLogs.CountAsync(x => x.Action == "RestockRecorded")));
        using var addLine = await PostAsync(browser, "/Restocks/Create?handler=AddLine", fields, $"/Restocks/Create?vendorId={vendor}");
        Assert.Equal(HttpStatusCode.OK, addLine.StatusCode);
        Assert.Contains("Input.Items[2].ProductId", await addLine.Content.ReadAsStringAsync());
        Assert.Equal(1, await fixture.ExecuteDbAsync(db => db.RestockEvents.CountAsync()));
    }

    [Fact]
    public async Task AdjustmentForm_RejectsNegativeStockAndZero_AndRecordsValidChangeWithActor()
    {
        await fixture.ResetDatabaseAsync();
        var (_, first, _) = await SeedCatalogAsync();
        using var browser = await LoginAsync();
        var fields = new Dictionary<string, string>
        {
            ["Input.ProductId"] = first.ToString(),
            ["Input.QuantityChange"] = "-11",
            ["Input.Reason"] = "Damage",
            ["Input.Notes"] = "Broken packaging"
        };
        using var negative = await PostAsync(browser, "/Adjustments/Create", fields);
        Assert.Equal(HttpStatusCode.Conflict, negative.StatusCode);
        Assert.Contains("below zero", await negative.Content.ReadAsStringAsync());
        fields["Input.QuantityChange"] = "0";
        using var zero = await PostAsync(browser, "/Adjustments/Create", fields);
        Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.Equal(0, await fixture.ExecuteDbAsync(db => db.InventoryAdjustments.CountAsync()));
        fields["Input.QuantityChange"] = "-2";
        using var saved = await PostAsync(browser, "/Adjustments/Create", fields);
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        using var details = await browser.GetAsync(saved.Headers.Location);
        Assert.Contains("Broken packaging", await details.Content.ReadAsStringAsync());
        Assert.Equal(8, await fixture.ExecuteDbAsync(db => db.Products.Where(x => x.Id == first).Select(x => x.QuantityOnHand).SingleAsync()));
        string actor = await fixture.GetUserIdAsync(ApiTestFixture.EmployeeEmail);
        Assert.Equal(actor, await fixture.ExecuteDbAsync(db => db.AuditLogs.Where(x => x.Action == "InventoryAdjusted").Select(x => x.UserId).SingleAsync()));
    }

    [Fact]
    public async Task AdminPages_CreateAndManageUsers_ProtectFinalAdmin_AndNeverEchoPasswords()
    {
        await fixture.ResetDatabaseAsync();
        using var browser = await LoginAsync(admin: true);
        const string password = "BrowserInitial1!";
        using var invalid = await PostAsync(browser, "/Users/Create", new()
        {
            ["Input.Email"] = "invalid",
            ["Input.DisplayName"] = "New person",
            ["Input.Role"] = "Employee",
            ["Input.InitialPassword"] = password
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.DoesNotContain(password, await invalid.Content.ReadAsStringAsync());
        using var create = await PostAsync(browser, "/Users/Create", new()
        {
            ["Input.Email"] = "browser@example.test",
            ["Input.DisplayName"] = "New person",
            ["Input.Role"] = "Employee",
            ["Input.InitialPassword"] = password
        });
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        string id = await fixture.GetUserIdAsync("browser@example.test");
        using var change = await PostAsync(browser, $"/Users/Edit/{id}", new() { ["Input.Role"] = "Manager" });
        Assert.Equal(HttpStatusCode.Redirect, change.StatusCode);
        using var deactivate = await PostAsync(browser, $"/Users/Edit/{id}?handler=Deactivate", new());
        Assert.Equal(HttpStatusCode.Redirect, deactivate.StatusCode);
        Assert.False(await fixture.ExecuteDbAsync(db => db.Users.Where(x => x.Id == id).Select(x => x.IsActive).SingleAsync()));
        using var reactivate = await PostAsync(browser, $"/Users/Edit/{id}?handler=Reactivate", new());
        Assert.Equal(HttpStatusCode.Redirect, reactivate.StatusCode);
        string adminId = await fixture.GetUserIdAsync(ApiTestFixture.AdminEmail);
        using var demote = await PostAsync(browser, $"/Users/Edit/{adminId}", new() { ["Input.Role"] = "Employee" });
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
        using var self = await PostAsync(browser, $"/Users/Edit/{adminId}?handler=Deactivate", new());
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        using var audit = await browser.GetAsync("/Audit");
        Assert.Equal(HttpStatusCode.OK, audit.StatusCode);
        int auditId = await fixture.ExecuteDbAsync(db => db.AuditLogs.Select(x => x.Id).FirstAsync());
        using var auditDetails = await browser.GetAsync($"/Audit/Details/{auditId}");
        Assert.Equal(HttpStatusCode.OK, auditDetails.StatusCode);
    }

    [Fact]
    public async Task ProductPagination_PreservesFilters_AndErrorsDoNotExposeInternals()
    {
        await fixture.ResetDatabaseAsync();
        await SeedCatalogAsync();
        using var browser = await LoginAsync(manager: true);
        using var page = await browser.GetAsync("/Products?Query.Search=Browser&Query.PageSize=1&Query.SortBy=sku");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        string html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Query.Page=2", html);
        Assert.Contains("Query.Search=Browser", html);
        Assert.Contains("Query.PageSize=1", html);
        using var invalid = await browser.GetAsync("/Products?Query.SortBy=untrusted");
        Assert.Equal(HttpStatusCode.Redirect, invalid.StatusCode);
        Assert.StartsWith("/Error?code=400&reference=", invalid.Headers.Location!.ToString());
        using var missing = await browser.GetAsync("/Products/Details/999999");
        Assert.Equal(HttpStatusCode.Redirect, missing.StatusCode);
        using var error = await browser.GetAsync(missing.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        string body = await error.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SqlException", body);
        Assert.DoesNotContain("SQLEXPRESS", body);
        Assert.DoesNotContain("StackTrace", body);
    }

    [Fact]
    public async Task PasswordForm_ValidatesCurrentPassword_RefreshesLogin_AndRecordsAudit()
    {
        await fixture.ResetDatabaseAsync();
        using var browser = await LoginAsync();
        const string replacement = "BrowserReplacement2!";
        using var invalid = await PostAsync(browser, "/Account/Password", new()
        {
            ["Input.CurrentPassword"] = "WrongPassword1!",
            ["Input.NewPassword"] = replacement
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.DoesNotContain(replacement, await invalid.Content.ReadAsStringAsync());
        using var saved = await PostAsync(browser, "/Account/Password", new()
        {
            ["Input.CurrentPassword"] = ApiTestFixture.EmployeePassword,
            ["Input.NewPassword"] = replacement
        });
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        using var dashboard = await browser.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        using var logout = await PostAsync(browser, "/Account/Logout", new());
        using var old = await PostAsync(browser, "/Account/Login", new()
        {
            ["Input.Email"] = ApiTestFixture.EmployeeEmail,
            ["Input.Password"] = ApiTestFixture.EmployeePassword
        });
        Assert.Equal(HttpStatusCode.BadRequest, old.StatusCode);
        using var login = await PostAsync(browser, "/Account/Login", new()
        {
            ["Input.Email"] = ApiTestFixture.EmployeeEmail,
            ["Input.Password"] = replacement
        });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal(1, await fixture.ExecuteDbAsync(db => db.AuditLogs.CountAsync(x => x.Action == "PasswordChanged")));
    }

    private HttpClient Browser() => fixture.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    });

    private async Task<HttpClient> LoginAsync(bool manager = false, bool admin = false)
    {
        var browser = Browser();
        using var login = await PostAsync(browser, "/Account/Login", new()
        {
            ["Input.Email"] = admin ? ApiTestFixture.AdminEmail : manager ? ApiTestFixture.ManagerEmail : ApiTestFixture.EmployeeEmail,
            ["Input.Password"] = admin ? ApiTestFixture.AdminPassword : manager ? ApiTestFixture.ManagerPassword : ApiTestFixture.EmployeePassword
        });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return browser;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient browser, string path,
        Dictionary<string, string> fields, string? tokenPage = null)
    {
        using var page = await browser.GetAsync(tokenPage ?? path);
        page.EnsureSuccessStatusCode();
        string html = await page.Content.ReadAsStringAsync();
        string input = Regex.Matches(html, "<input\\b[^>]*>", RegexOptions.IgnoreCase)
            .Select(x => x.Value).First(x => x.Contains("name=\"__RequestVerificationToken\"", StringComparison.Ordinal));
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(Regex.Match(input, "value=\"([^\"]*)\"").Groups[1].Value);
        return await browser.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    private static Dictionary<string, string> ProductFields(int category, int vendor) => new()
    {
        ["Input.Name"] = "Browser product",
        ["Input.Sku"] = "BROWSER-NEW",
        ["Input.CategoryId"] = category.ToString(),
        ["Input.PrimaryVendorId"] = vendor.ToString(),
        ["Input.ReorderThreshold"] = "5",
        ["Input.Cost"] = "1.50",
        ["Input.RetailPrice"] = "2.50"
    };

    private Task<(int Vendor, int First, int Second)> SeedCatalogAsync() => fixture.ExecuteDbAsync(async db =>
    {
        var now = DateTime.UtcNow;
        var category = new Category { Name = "Browser category", CreatedAtUtc = now, UpdatedAtUtc = now };
        var vendor = new Vendor { Name = "Browser vendor", CreatedAtUtc = now, UpdatedAtUtc = now };
        var first = new Product
        {
            Name = "Browser product one",
            Sku = "BROWSER-1",
            Category = category,
            PrimaryVendor = vendor,
            QuantityOnHand = 10,
            ReorderThreshold = 10,
            Cost = 1,
            RetailPrice = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var second = new Product
        {
            Name = "Browser product two",
            Sku = "BROWSER-2",
            Category = category,
            PrimaryVendor = vendor,
            QuantityOnHand = 10,
            ReorderThreshold = 5,
            Cost = 1,
            RetailPrice = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.Products.AddRange(first, second);
        await db.SaveChangesAsync();
        return (vendor.Id, first.Id, second.Id);
    });
}
