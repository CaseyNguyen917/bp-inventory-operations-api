using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Data;
using BPInventoryOps.Api.Dtos.AuditLogs;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Entities;
using BPInventoryOps.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BPInventoryOps.Api.Services;

public sealed class AuditService(
    ApplicationDbContext dbContext,
    ICurrentUserContext currentUserContext) : IAuditService
{
    public async Task<PagedResponse<AuditLogResponse>> ListAsync(
        AuditLogListQuery request,
        CancellationToken cancellationToken)
    {
        ValidateDateRange(request.FromUtc, request.ToUtc);

        IQueryable<AuditLog> query = dbContext.AuditLogs.AsNoTracking();

        string? userId = NormalizeOptional(request.UserId);
        if (userId is not null)
        {
            query = query.Where(auditLog => auditLog.UserId == userId);
        }

        string? action = NormalizeOptional(request.Action);
        if (action is not null)
        {
            query = query.Where(auditLog => auditLog.Action == action);
        }

        string? entityType = NormalizeOptional(request.EntityType);
        if (entityType is not null)
        {
            query = query.Where(auditLog => auditLog.EntityType == entityType);
        }

        string? entityId = NormalizeOptional(request.EntityId);
        if (entityId is not null)
        {
            query = query.Where(auditLog => auditLog.EntityId == entityId);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(auditLog => auditLog.TimestampUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(auditLog => auditLog.TimestampUtc <= request.ToUtc.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<AuditLogResponse> items = await query
            .OrderByDescending(auditLog => auditLog.TimestampUtc)
            .ThenByDescending(auditLog => auditLog.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(auditLog => new AuditLogResponse
            {
                Id = auditLog.Id,
                User = auditLog.User == null
                    ? null
                    : new UserSummaryResponse(auditLog.User.Id, auditLog.User.DisplayName),
                Action = auditLog.Action,
                EntityType = auditLog.EntityType,
                EntityId = auditLog.EntityId,
                Details = auditLog.Details,
                TimestampUtc = auditLog.TimestampUtc
            })
            .ToListAsync(cancellationToken);

        return PagedResponse<AuditLogResponse>.Create(
            items,
            request.Page,
            request.PageSize,
            totalCount);
    }

    public async Task<AuditLogResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        AuditLogResponse? response = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(auditLog => auditLog.Id == id)
            .Select(auditLog => new AuditLogResponse
            {
                Id = auditLog.Id,
                User = auditLog.User == null
                    ? null
                    : new UserSummaryResponse(auditLog.User.Id, auditLog.User.DisplayName),
                Action = auditLog.Action,
                EntityType = auditLog.EntityType,
                EntityId = auditLog.EntityId,
                Details = auditLog.Details,
                TimestampUtc = auditLog.TimestampUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException($"Audit Log {id} was not found.");
    }

    public void Add(
        string action,
        string entityType,
        string? entityId,
        string? details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = currentUserContext.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            TimestampUtc = DateTime.UtcNow
        });
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
