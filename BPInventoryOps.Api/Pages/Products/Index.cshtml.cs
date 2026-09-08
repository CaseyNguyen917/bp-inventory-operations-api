using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Dtos.Products;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace BPInventoryOps.Api.Pages.Products;
public sealed class IndexModel(IProductService products, PageLookups lookups) : UiPageModel
{
    [BindProperty(Name = "Query", SupportsGet = true)] public ProductListQuery Query { get; set; } = new();
    public PagedResponse<ProductResponse> Data { get; private set; } = null!;
    public List<SelectListItem> Categories { get; private set; } = [];
    public List<SelectListItem> Vendors { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InvalidQuery() is { } invalid) return invalid;
        Data = await products.ListAsync(Query, ct);
        Categories = await lookups.CategoriesAsync(ct);
        Vendors = await lookups.VendorsAsync(ct);
        return Page();
    }
}
