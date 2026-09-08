using BPInventoryOps.Api.Dtos.Restocks;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
namespace BPInventoryOps.Api.Pages.Restocks;
public sealed class DetailsModel(IRestockService service) : UiPageModel
{
    public RestockResponse Item { get; private set; } = null!;
    public async Task OnGetAsync(int id, CancellationToken ct) => Item = await service.GetByIdAsync(id, ct);
}
