using System.Data;
using System.Text.Json;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.InventoryAdjustments;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BPInventoryOps.Api.Services;

public sealed class InventoryAdjustmentService(
    ApplicationDbContext dbContext,
    IAuditService auditService,
    ICurrentUserContext currentUserContext,
    ILogger<InventoryAdjustmentService> logger) : IInventoryAdjustmentService
{
    public async Task<PagedResponse<InventoryAdjustmentResponse>> ListAsync(
        InventoryAdjustmentListQuery request,
        CancellationToken cancellationToken)
    {
        ValidateDateRange(request.FromUtc, request.ToUtc);

        IQueryable<InventoryAdjustment> query = dbContext.InventoryAdjustments.AsNoTracking();

        if (request.ProductId.HasValue)
        {
            query = query.Where(adjustment => adjustment.ProductId == request.ProductId.Value);
        }

        if (request.Reason.HasValue)
        {
            query = query.Where(adjustment => adjustment.Reason == request.Reason.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(adjustment => adjustment.RecordedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(adjustment => adjustment.RecordedAtUtc <= request.ToUtc.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<InventoryAdjustmentResponse> items = await query
            .OrderByDescending(adjustment => adjustment.RecordedAtUtc)
            .ThenByDescending(adjustment => adjustment.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(adjustment => new InventoryAdjustmentResponse
            {
                Id = adjustment.Id,
                Product = new ProductSummaryResponse(
                    adjustment.Product.Id,
                    adjustment.Product.Name,
                    adjustment.Product.Sku),
                QuantityChange = adjustment.QuantityChange,
                Reason = adjustment.Reason,
                Notes = adjustment.Notes,
                RecordedBy = new UserSummaryResponse(
                    adjustment.RecordedByUser.Id,
                    adjustment.RecordedByUser.DisplayName),
                RecordedAtUtc = adjustment.RecordedAtUtc
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<InventoryAdjustmentResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<InventoryAdjustmentResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        InventoryAdjustmentResponse? response = await dbContext.InventoryAdjustments
            .AsNoTracking()
            .Where(adjustment => adjustment.Id == id)
            .Select(adjustment => new InventoryAdjustmentResponse
            {
                Id = adjustment.Id,
                Product = new ProductSummaryResponse(
                    adjustment.Product.Id,
                    adjustment.Product.Name,
                    adjustment.Product.Sku),
                QuantityChange = adjustment.QuantityChange,
                Reason = adjustment.Reason,
                Notes = adjustment.Notes,
                RecordedBy = new UserSummaryResponse(
                    adjustment.RecordedByUser.Id,
                    adjustment.RecordedByUser.DisplayName),
                RecordedAtUtc = adjustment.RecordedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return response
            ?? throw new NotFoundException($"Inventory Adjustment {id} was not found.");
    }

    public async Task<InventoryAdjustmentResponse> CreateAsync(
        CreateInventoryAdjustmentRequest request,
        CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);

        await using IDbContextTransaction transaction = await dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            Product product = await dbContext.Products
                .SingleOrDefaultAsync(item => item.Id == request.ProductId, cancellationToken)
                ?? throw new NotFoundException($"Product {request.ProductId} was not found.");

            if (!product.IsActive)
            {
                throw new ConflictException(
                    "Inventory cannot be adjusted for an inactive Product.");
            }

            int previousQuantity = product.QuantityOnHand;
            long newQuantity = (long)previousQuantity + request.QuantityChange;

            if (newQuantity < 0)
            {
                throw new ConflictException(
                    "The adjustment would reduce inventory below zero.");
            }

            if (newQuantity > int.MaxValue)
            {
                throw new ConflictException(
                    "The adjustment would exceed the supported inventory quantity.");
            }

            string actorUserId = currentUserContext.UserId;
            DateTime now = DateTime.UtcNow;

            product.QuantityOnHand = (int)newQuantity;
            product.UpdatedAtUtc = now;

            InventoryAdjustment adjustment = new()
            {
                ProductId = product.Id,
                RecordedByUserId = actorUserId,
                QuantityChange = request.QuantityChange,
                Reason = request.Reason!.Value,
                Notes = NormalizeOptional(request.Notes),
                RecordedAtUtc = now
            };

            dbContext.InventoryAdjustments.Add(adjustment);
            await dbContext.SaveChangesAsync(cancellationToken);

            auditService.Add(
                AuditActions.InventoryAdjusted,
                nameof(InventoryAdjustment),
                adjustment.Id.ToString(),
                JsonSerializer.Serialize(new
                {
                    adjustment.ProductId,
                    adjustment.QuantityChange,
                    Reason = adjustment.Reason.ToString(),
                    PreviousQuantity = previousQuantity,
                    NewQuantity = product.QuantityOnHand
                }));

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Recorded Inventory Adjustment {AdjustmentId} for Product {ProductId} with change {QuantityChange} by actor {UserId}",
                adjustment.Id,
                adjustment.ProductId,
                adjustment.QuantityChange,
                actorUserId);

            return await GetByIdAsync(adjustment.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static void ValidateCreateRequest(CreateInventoryAdjustmentRequest request)
    {
        if (request.QuantityChange == 0)
        {
            throw new RequestValidationException("quantityChange must not be zero.");
        }

        if (!request.Reason.HasValue
            || !Enum.IsDefined(request.Reason.Value))
        {
            throw new RequestValidationException("reason must be a supported AdjustmentReason.");
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
