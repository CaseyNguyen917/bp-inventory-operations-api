using BPInventoryOps.Api.Dtos.InventoryAdjustments;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace BPInventoryOps.Api.Pages.Adjustments;
public sealed class IndexModel(IInventoryAdjustmentService service, PageLookups lookups) : UiPageModel
{
    [BindProperty(Name = "Query", SupportsGet = true)] public InventoryAdjustmentListQuery Query { get; set; } = new();
    public PagedResponse<InventoryAdjustmentResponse> Data { get; private set; } = null!;
    public List<SelectListItem> Products { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InvalidQuery() is { } invalid) return invalid;
        Data = await service.ListAsync(Query, ct);
        Products = await lookups.ProductsAsync(ct, includeInactive: true);
        return Page();
    }
}
