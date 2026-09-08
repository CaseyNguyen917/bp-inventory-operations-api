using BPInventoryOps.Api.Dtos.InventoryAdjustments;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace BPInventoryOps.Api.Pages.Adjustments;
public sealed class CreateModel(IInventoryAdjustmentService service, PageLookups lookups) : UiPageModel
{
    [BindProperty] public CreateInventoryAdjustmentRequest Input { get; set; } = new();
    public List<SelectListItem> Products { get; private set; } = [];
    public async Task OnGetAsync(int? productId, CancellationToken ct)
    {
        Input = new() { ProductId = productId ?? 0 };
        await LoadAsync(ct);
    }
    private async Task LoadAsync(CancellationToken ct) => Products = await lookups.ProductsAsync(ct);
    public Task<IActionResult> OnPostAsync(CancellationToken ct) => SaveAsync(async () =>
    {
        var item = await service.CreateAsync(Input, ct);
        return Saved("Adjustment recorded. Stock and audit history have been updated.", "/Adjustments/Details", new { id = item.Id });
    }, () => LoadAsync(ct));
}
