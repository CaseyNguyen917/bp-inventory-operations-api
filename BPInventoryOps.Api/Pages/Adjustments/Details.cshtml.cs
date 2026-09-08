using BPInventoryOps.Api.Dtos.InventoryAdjustments;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
namespace BPInventoryOps.Api.Pages.Adjustments;
public sealed class DetailsModel(IInventoryAdjustmentService service) : UiPageModel
{
    public InventoryAdjustmentResponse Item { get; private set; } = null!;
    public async Task OnGetAsync(int id, CancellationToken ct) => Item = await service.GetByIdAsync(id, ct);
}
