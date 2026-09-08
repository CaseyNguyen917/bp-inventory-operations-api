using BPInventoryOps.Api.Dtos.Restocks;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace BPInventoryOps.Api.Pages.Restocks;
public sealed class CreateModel(IRestockService service, PageLookups lookups) : UiPageModel
{
    [BindProperty] public RestockForm Input { get; set; } = new();
    public List<SelectListItem> Vendors { get; private set; } = [];
    public List<SelectListItem> Products { get; private set; } = [];
    private async Task LoadAsync(CancellationToken ct)
    {
        Vendors = await lookups.VendorsAsync(ct);
        foreach (var vendor in Vendors) vendor.Selected = vendor.Value == Input.VendorId.ToString();
        if (Input.VendorId > 0) Products = await lookups.ProductsAsync(ct, Input.VendorId);
        if (Input.Items.Count == 0) Input.Items.Add(new() { QuantityReceived = 1 });
    }
    public async Task OnGetAsync(int? vendorId, CancellationToken ct)
    {
        Input = new() { VendorId = vendorId ?? 0, ReceivedAtUtc = DateTime.UtcNow, Items = [new() { QuantityReceived = 1 }] };
        await LoadAsync(ct);
    }
    public Task<IActionResult> OnPostAsync(CancellationToken ct) => SaveAsync(async () =>
    {
        var item = await service.CreateAsync(new CreateRestockRequest
        {
            VendorId = Input.VendorId, Notes = Input.Notes, Items = Input.Items,
            ReceivedAtUtc = Input.ReceivedAtUtc.HasValue ? DateTime.SpecifyKind(Input.ReceivedAtUtc.Value, DateTimeKind.Utc) : null
        }, ct);
        return Saved("Delivery recorded. Stock and audit history have been updated.", "/Restocks/Details", new { id = item.Id });
    }, () => LoadAsync(ct));
    // Server-rendered fallback when JavaScript is disabled.
    public async Task<IActionResult> OnPostAddLineAsync(CancellationToken ct)
    {
        Input = new() { VendorId = Input.VendorId, ReceivedAtUtc = Input.ReceivedAtUtc, Notes = Input.Notes,
            Items = [..Input.Items, new() { QuantityReceived = 1 }] };
        ModelState.Clear();
        await LoadAsync(ct);
        return Page();
    }
}
