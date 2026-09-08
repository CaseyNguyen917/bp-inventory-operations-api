using BPInventoryOps.Api.Dtos.AuditLogs;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace BPInventoryOps.Api.Pages.Audit;
public sealed class IndexModel(IAuditService service) : UiPageModel
{
    [BindProperty(Name = "Query", SupportsGet = true)] public AuditLogListQuery Query { get; set; } = new();
    public PagedResponse<AuditLogResponse> Data { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InvalidQuery() is { } invalid) return invalid;
        Data = await service.ListAsync(Query, ct);
        return Page();
    }
}
