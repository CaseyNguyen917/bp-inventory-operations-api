using BPInventoryOps.Api.Dtos.Common;

namespace BPInventoryOps.Api.Dtos.AuditLogs;

public sealed class AuditLogResponse
{
    public required int Id { get; init; }

    public UserSummaryResponse? User { get; init; }

    public required string Action { get; init; }

    public required string EntityType { get; init; }

    public string? EntityId { get; init; }

    public string? Details { get; init; }

    public required DateTime TimestampUtc { get; init; }
}
