using BPInventoryOps.Api.Dtos.Restocks;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace BPInventoryOps.Api.Pages.Restocks;
public sealed class IndexModel(IRestockService service, PageLookups lookups) : UiPageModel
{
    [BindProperty(Name = "Query", SupportsGet = true)] public RestockListQuery Query { get; set; } = new();
    public PagedResponse<RestockSummaryResponse> Data { get; private set; } = null!;
    public List<SelectListItem> Products { get; private set; } = [];
    public List<SelectListItem> Vendors { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InvalidQuery() is { } invalid) return invalid;
        Data = await service.ListAsync(Query, ct);
        Products = await lookups.ProductsAsync(ct, includeInactive: true);
        Vendors = await lookups.VendorsAsync(ct, includeInactive: true);
        return Page();
    }
}
