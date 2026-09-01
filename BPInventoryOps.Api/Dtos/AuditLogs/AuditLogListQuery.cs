using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.AuditLogs;

public sealed class AuditLogListQuery : PaginationQuery
{
    public string? UserId { get; init; }

    public string? Action { get; init; }

    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }
}
