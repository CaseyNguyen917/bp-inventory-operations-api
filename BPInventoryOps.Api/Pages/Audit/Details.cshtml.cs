using BPInventoryOps.Api.Dtos.AuditLogs;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
namespace BPInventoryOps.Api.Pages.Audit;
public sealed class DetailsModel(IAuditService service) : UiPageModel
{
    public AuditLogResponse Item { get; private set; } = null!;
    public async Task OnGetAsync(int id, CancellationToken ct) => Item = await service.GetByIdAsync(id, ct);
}
