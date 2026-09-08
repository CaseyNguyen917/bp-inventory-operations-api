using BPInventoryOps.Api.Dtos.Vendors;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Pages.Vendors;
public sealed class EditModel(IVendorService service) : UiPageModel
{
    [BindProperty] public CreateVendorRequest Input { get; set; } = new();
    public VendorResponse? Existing { get; private set; }
    
    private async Task LoadAsync(int? id, CancellationToken ct)
    {
        if (id.HasValue) Existing = await service.GetByIdAsync(id.Value, ct);
        
    }
    public async Task OnGetAsync(int? id, CancellationToken ct)
    {
        await LoadAsync(id, ct);
        if (Existing is { } item) Input = new() { Name = item.Name, ContactName = item.ContactName, Phone = item.Phone, Email = item.Email };
    }
    public Task<IActionResult> OnPostAsync(int? id, CancellationToken ct) => SaveAsync(async () =>
    {
        var item = id.HasValue
            ? await service.UpdateAsync(id.Value, new UpdateVendorRequest { Name = Input.Name, ContactName = Input.ContactName, Phone = Input.Phone, Email = Input.Email }, ct)
            : await service.CreateAsync(Input, ct);
        return Saved("Vendor saved.", "/Vendors/Index", null);
    }, () => LoadAsync(id, ct));
    public Task<IActionResult> OnPostDeactivateAsync(int id, CancellationToken ct) => ChangeStatusAsync(id, false, ct);
    public Task<IActionResult> OnPostReactivateAsync(int id, CancellationToken ct) => ChangeStatusAsync(id, true, ct);
    private Task<IActionResult> ChangeStatusAsync(int id, bool active, CancellationToken ct)
    {
        ModelState.Clear(); // This separate form binds no editable catalog fields.
        return SaveAsync(async () =>
        {
            if (active) await service.ReactivateAsync(id, ct);
            else await service.DeactivateAsync(id, ct);
            return Saved("Vendor " + (active ? "reactivated." : "deactivated."), "/Vendors/Index");
        }, () => OnGetAsync(id, ct));
    }
}

