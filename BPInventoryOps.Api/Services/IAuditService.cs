using BPInventoryOps.Api.Dtos.AuditLogs;
using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Services;

public interface IAuditService
{
    Task<PagedResponse<AuditLogResponse>> ListAsync(
        AuditLogListQuery request,
        CancellationToken cancellationToken);

    Task<AuditLogResponse> GetByIdAsync(int id, CancellationToken cancellationToken);

    void Add(
        string action,
        string entityType,
        string? entityId,
        string? details);
}
