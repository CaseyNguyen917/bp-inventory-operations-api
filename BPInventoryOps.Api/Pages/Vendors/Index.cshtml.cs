using BPInventoryOps.Api.Dtos.Vendors;
using BPInventoryOps.Api.Dtos.Common;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace BPInventoryOps.Api.Pages.Vendors;
public sealed class IndexModel(IVendorService service) : UiPageModel
{
    [BindProperty(Name = "Query", SupportsGet = true)] public VendorListQuery Query { get; set; } = new();
    public PagedResponse<VendorResponse> Data { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InvalidQuery() is { } invalid) return invalid;
        Data = await service.ListAsync(Query, ct);
        return Page();
    }
}
