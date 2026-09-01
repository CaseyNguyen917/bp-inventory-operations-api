using System.Text.Json;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Products;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BPInventoryOps.Api.Services;

public sealed class ProductService(
    ApplicationDbContext dbContext,
    IAuditService auditService,
    ICurrentUserContext currentUserContext,
    ILogger<ProductService> logger) : IProductService
{
    private static readonly string[] AllowedSortFields =
        ["name", "sku", "quantityonhand", "retailprice"];

    public async Task<PagedResponse<ProductResponse>> ListAsync(
        ProductListQuery request,
        CancellationToken cancellationToken)
    {
        string sortBy = ValidateSort(request.SortBy, request.SortDirection);

        IQueryable<Product> query = dbContext.Products.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(product => product.IsActive);
        }

        string? search = NormalizeOptional(request.Search);
        if (search is not null)
        {
            query = query.Where(product =>
                product.Name.Contains(search) || product.Sku.Contains(search));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        }

        if (request.VendorId.HasValue)
        {
            query = query.Where(product => product.PrimaryVendorId == request.VendorId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = ApplySort(query, sortBy, request.SortDirection);

        List<ProductResponse> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                Category = new CategorySummaryResponse(
                    product.Category.Id,
                    product.Category.Name),
                PrimaryVendor = new VendorSummaryResponse(
                    product.PrimaryVendor.Id,
                    product.PrimaryVendor.Name),
                QuantityOnHand = product.QuantityOnHand,
                ReorderThreshold = product.ReorderThreshold,
                Cost = product.Cost,
                RetailPrice = product.RetailPrice,
                IsLowStock = product.QuantityOnHand <= product.ReorderThreshold,
                IsActive = product.IsActive,
                CreatedAtUtc = product.CreatedAtUtc,
                UpdatedAtUtc = product.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<ProductResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<PagedResponse<LowStockProductResponse>> ListLowStockAsync(
        LowStockProductQuery request,
        CancellationToken cancellationToken)
    {
        string sortBy = ValidateSort(request.SortBy, request.SortDirection);

        IQueryable<Product> query = dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.IsActive
                && product.QuantityOnHand <= product.ReorderThreshold);

        if (request.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == request.CategoryId.Value);
        }

        if (request.VendorId.HasValue)
        {
            query = query.Where(product => product.PrimaryVendorId == request.VendorId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        query = ApplySort(query, sortBy, request.SortDirection);

        List<LowStockProductResponse> items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new LowStockProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                QuantityOnHand = product.QuantityOnHand,
                ReorderThreshold = product.ReorderThreshold,
                PrimaryVendor = new VendorSummaryResponse(
                    product.PrimaryVendor.Id,
                    product.PrimaryVendor.Name)
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<LowStockProductResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<ProductResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        ProductResponse? product = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ProductResponse
            {
                Id = item.Id,
                Name = item.Name,
                Sku = item.Sku,
                Category = new CategorySummaryResponse(item.Category.Id, item.Category.Name),
                PrimaryVendor = new VendorSummaryResponse(
                    item.PrimaryVendor.Id,
                    item.PrimaryVendor.Name),
                QuantityOnHand = item.QuantityOnHand,
                ReorderThreshold = item.ReorderThreshold,
                Cost = item.Cost,
                RetailPrice = item.RetailPrice,
                IsLowStock = item.QuantityOnHand <= item.ReorderThreshold,
                IsActive = item.IsActive,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return product ?? throw new NotFoundException($"Product {id} was not found.");
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        string name = request.Name.Trim();
        string sku = request.Sku.Trim();

        await ValidateMasterDataAsync(
            request.CategoryId,
            request.PrimaryVendorId,
            cancellationToken);

        if (await dbContext.Products.AnyAsync(
                product => product.Sku == sku,
                cancellationToken))
        {
            throw new ConflictException($"A Product with SKU '{sku}' already exists.");
        }

        DateTime now = DateTime.UtcNow;
        Product product = new()
        {
            Name = name,
            Sku = sku,
            CategoryId = request.CategoryId,
            PrimaryVendorId = request.PrimaryVendorId,
            QuantityOnHand = 0,
            ReorderThreshold = request.ReorderThreshold,
            Cost = request.Cost,
            RetailPrice = request.RetailPrice,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        dbContext.Products.Add(product);

        string actorUserId = currentUserContext.UserId;
        auditService.Add(
            AuditActions.ProductCreated,
            nameof(Product),
            null,
            JsonSerializer.Serialize(new { product.Name, product.Sku }));

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created Product {ProductId} by actor {UserId}",
            product.Id,
            actorUserId);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        Product product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Product {id} was not found.");

        string sku = request.Sku.Trim();

        await ValidateMasterDataAsync(
            request.CategoryId,
            request.PrimaryVendorId,
            cancellationToken);

        if (await dbContext.Products.AnyAsync(
                item => item.Id != id && item.Sku == sku,
                cancellationToken))
        {
            throw new ConflictException($"A Product with SKU '{sku}' already exists.");
        }

        product.Name = request.Name.Trim();
        product.Sku = sku;
        product.CategoryId = request.CategoryId;
        product.PrimaryVendorId = request.PrimaryVendorId;
        product.ReorderThreshold = request.ReorderThreshold;
        product.Cost = request.Cost;
        product.RetailPrice = request.RetailPrice;
        product.UpdatedAtUtc = DateTime.UtcNow;

        string actorUserId = currentUserContext.UserId;
        auditService.Add(
            AuditActions.ProductUpdated,
            nameof(Product),
            product.Id.ToString(),
            JsonSerializer.Serialize(new { product.Name, product.Sku }));

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        Product product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Product {id} was not found.");

        if (!product.IsActive)
        {
            return;
        }

        product.IsActive = false;
        product.UpdatedAtUtc = DateTime.UtcNow;

        string actorUserId = currentUserContext.UserId;
        auditService.Add(
            AuditActions.ProductDeactivated,
            nameof(Product),
            product.Id.ToString(),
            null);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deactivated Product {ProductId} by actor {UserId}",
            product.Id,
            actorUserId);
    }

    public async Task<ProductResponse> ReactivateAsync(
        int id,
        CancellationToken cancellationToken)
    {
        Product product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Product {id} was not found.");

        if (product.IsActive)
        {
            return await GetByIdAsync(product.Id, cancellationToken);
        }

        await ValidateMasterDataAsync(
            product.CategoryId,
            product.PrimaryVendorId,
            cancellationToken);

        product.IsActive = true;
        product.UpdatedAtUtc = DateTime.UtcNow;

        string actorUserId = currentUserContext.UserId;
        auditService.Add(
            AuditActions.ProductReactivated,
            nameof(Product),
            product.Id.ToString(),
            null);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reactivated Product {ProductId} by actor {UserId}",
            product.Id,
            actorUserId);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    private async Task ValidateMasterDataAsync(
        int categoryId,
        int vendorId,
        CancellationToken cancellationToken)
    {
        bool? categoryIsActive = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == categoryId)
            .Select(category => (bool?)category.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (!categoryIsActive.HasValue)
        {
            throw new NotFoundException($"Category {categoryId} was not found.");
        }

        if (!categoryIsActive.Value)
        {
            throw new ConflictException("The selected Category is inactive.");
        }

        bool? vendorIsActive = await dbContext.Vendors
            .AsNoTracking()
            .Where(vendor => vendor.Id == vendorId)
            .Select(vendor => (bool?)vendor.IsActive)
            .SingleOrDefaultAsync(cancellationToken);

        if (!vendorIsActive.HasValue)
        {
            throw new NotFoundException($"Vendor {vendorId} was not found.");
        }

        if (!vendorIsActive.Value)
        {
            throw new ConflictException("The selected Primary Vendor is inactive.");
        }
    }

    private static IQueryable<Product> ApplySort(
        IQueryable<Product> query,
        string sortBy,
        string sortDirection)
    {
        bool descending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, descending) switch
        {
            ("name", false) => query.OrderBy(product => product.Name).ThenBy(product => product.Id),
            ("name", true) => query.OrderByDescending(product => product.Name)
                .ThenByDescending(product => product.Id),
            ("sku", false) => query.OrderBy(product => product.Sku).ThenBy(product => product.Id),
            ("sku", true) => query.OrderByDescending(product => product.Sku)
                .ThenByDescending(product => product.Id),
            ("quantityonhand", false) => query.OrderBy(product => product.QuantityOnHand)
                .ThenBy(product => product.Id),
            ("quantityonhand", true) => query.OrderByDescending(product => product.QuantityOnHand)
                .ThenByDescending(product => product.Id),
            ("retailprice", false) => query.OrderBy(product => product.RetailPrice)
                .ThenBy(product => product.Id),
            _ => query.OrderByDescending(product => product.RetailPrice)
                .ThenByDescending(product => product.Id)
        };
    }

    private static string ValidateSort(string sortBy, string sortDirection)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            throw new RequestValidationException(
                "Product sortBy must be one of: name, sku, quantityOnHand, retailPrice.");
        }

        string normalizedSortBy = sortBy.Trim().ToLowerInvariant();

        if (!AllowedSortFields.Contains(normalizedSortBy))
        {
            throw new RequestValidationException(
                "Product sortBy must be one of: name, sku, quantityOnHand, retailPrice.");
        }

        if (string.IsNullOrWhiteSpace(sortDirection)
            || (!sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
                && !sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)))
        {
            throw new RequestValidationException("sortDirection must be 'asc' or 'desc'.");
        }

        return normalizedSortBy;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
