using System.Data;
using System.Text.Json;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Restocks;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BPInventoryOps.Api.Services;

public sealed class RestockService(
    ApplicationDbContext dbContext,
    IAuditService auditService,
    ICurrentUserContext currentUserContext,
    ILogger<RestockService> logger) : IRestockService
{
    public async Task<PagedResponse<RestockSummaryResponse>> ListAsync(
        RestockListQuery request,
        CancellationToken cancellationToken)
    {
        ValidateDateRange(request.FromUtc, request.ToUtc);

        IQueryable<RestockEvent> query = dbContext.RestockEvents.AsNoTracking();

        if (request.VendorId.HasValue)
        {
            query = query.Where(restock => restock.VendorId == request.VendorId.Value);
        }

        if (request.ProductId.HasValue)
        {
            query = query.Where(restock =>
                restock.Items.Any(item => item.ProductId == request.ProductId.Value));
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(restock => restock.ReceivedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(restock => restock.ReceivedAtUtc <= request.ToUtc.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<RestockSummaryResponse> items = await query
            .OrderByDescending(restock => restock.ReceivedAtUtc)
            .ThenByDescending(restock => restock.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(restock => new RestockSummaryResponse
            {
                Id = restock.Id,
                Vendor = new VendorSummaryResponse(restock.Vendor.Id, restock.Vendor.Name),
                ReceivedAtUtc = restock.ReceivedAtUtc,
                ItemCount = restock.Items.Count,
                TotalUnitsReceived = restock.Items.Sum(item => item.QuantityReceived)
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<RestockSummaryResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<RestockResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        RestockResponse? response = await dbContext.RestockEvents
            .AsNoTracking()
            .Where(restock => restock.Id == id)
            .Select(restock => new RestockResponse
            {
                Id = restock.Id,
                Vendor = new VendorSummaryResponse(restock.Vendor.Id, restock.Vendor.Name),
                RecordedBy = new UserSummaryResponse(
                    restock.RecordedByUser.Id,
                    restock.RecordedByUser.DisplayName),
                ReceivedAtUtc = restock.ReceivedAtUtc,
                Notes = restock.Notes,
                CreatedAtUtc = restock.CreatedAtUtc,
                Items = restock.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new RestockItemResponse
                    {
                        Product = new ProductSummaryResponse(
                            item.Product.Id,
                            item.Product.Name,
                            item.Product.Sku),
                        QuantityReceived = item.QuantityReceived
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException($"Restock {id} was not found.");
    }

    public async Task<RestockResponse> CreateAsync(
        CreateRestockRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            Vendor vendor = await dbContext.Vendors
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == request.VendorId, cancellationToken)
                ?? throw new NotFoundException($"Vendor {request.VendorId} was not found.");

            if (!vendor.IsActive)
            {
                throw new ConflictException("Restocks cannot be recorded for an inactive Vendor.");
            }

            int[] productIds = request.Items.Select(item => item.ProductId).ToArray();
            List<Product> products = await dbContext.Products
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync(cancellationToken);

            Dictionary<int, Product> productsById = products.ToDictionary(product => product.Id);

            foreach (int productId in productIds)
            {
                if (!productsById.TryGetValue(productId, out Product? product))
                {
                    throw new NotFoundException($"Product {productId} was not found.");
                }

                if (!product.IsActive)
                {
                    throw new ConflictException(
                        $"Product {product.Id} is inactive and cannot be restocked.");
                }

                if (product.PrimaryVendorId != request.VendorId)
                {
                    throw new ConflictException(
                        $"Product {product.Id} does not use Vendor {request.VendorId} as its Primary Vendor.");
                }
            }

            string actorUserId = currentUserContext.UserId;
            DateTime now = DateTime.UtcNow;

            RestockEvent restock = new()
            {
                VendorId = request.VendorId,
                RecordedByUserId = actorUserId,
                ReceivedAtUtc = request.ReceivedAtUtc!.Value,
                Notes = NormalizeOptional(request.Notes),
                CreatedAtUtc = now,
                Items = request.Items.Select(item => new RestockItem
                {
                    ProductId = item.ProductId,
                    QuantityReceived = item.QuantityReceived
                }).ToList()
            };

            foreach (RestockItemRequest item in request.Items)
            {
                Product product = productsById[item.ProductId];
                long newQuantity = (long)product.QuantityOnHand + item.QuantityReceived;

                if (newQuantity > int.MaxValue)
                {
                    throw new ConflictException(
                        $"Restocking Product {product.Id} would exceed the supported inventory quantity.");
                }

                product.QuantityOnHand = (int)newQuantity;
                product.UpdatedAtUtc = now;
            }

            dbContext.RestockEvents.Add(restock);
            await dbContext.SaveChangesAsync(cancellationToken);

            int totalUnitsReceived = request.Items.Sum(item => item.QuantityReceived);
            auditService.Add(
                AuditActions.RestockRecorded,
                nameof(RestockEvent),
                restock.Id.ToString(),
                JsonSerializer.Serialize(new
                {
                    restock.VendorId,
                    ItemCount = request.Items.Count,
                    TotalUnitsReceived = totalUnitsReceived
                }));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Recorded Restock {RestockId} for Vendor {VendorId} with {ItemCount} items by actor {UserId}",
                restock.Id,
                restock.VendorId,
                request.Items.Count,
                actorUserId);

            return await GetByIdAsync(restock.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void ValidateCreateRequest(CreateRestockRequest request)
    {
        if (!request.ReceivedAtUtc.HasValue)
        {
            throw new RequestValidationException("receivedAtUtc is required.");
        }

        if (request.ReceivedAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new RequestValidationException("receivedAtUtc must be an ISO-8601 UTC timestamp.");
        }

        if (request.Items.Count == 0)
        {
            throw new RequestValidationException("A Restock must contain at least one item.");
        }

        if (request.Items.Any(item => item.ProductId <= 0 || item.QuantityReceived <= 0))
        {
            throw new RequestValidationException(
                "Every Restock item requires a positive productId and quantityReceived.");
        }

        int distinctProductCount = request.Items
            .Select(item => item.ProductId)
            .Distinct()
            .Count();

        if (distinctProductCount != request.Items.Count)
        {
            throw new RequestValidationException(
                "A Restock cannot contain duplicate Product lines.");
        }

        long totalUnitsReceived = request.Items.Sum(item => (long)item.QuantityReceived);
        if (totalUnitsReceived > int.MaxValue)
        {
            throw new RequestValidationException(
                "The Restock total exceeds the supported quantity range.");
        }
    }

    private static void ValidateDateRange(DateTime? fromUtc, DateTime? toUtc)
    {
        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value > toUtc.Value)
        {
            throw new RequestValidationException("fromUtc cannot be later than toUtc.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
