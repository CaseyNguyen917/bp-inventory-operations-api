using BPInventoryOps.Api.Dtos.Products;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
namespace BPInventoryOps.Api.Pages.Products;
public sealed class EditModel(IProductService service, PageLookups lookups) : UiPageModel
{
    [BindProperty] public CreateProductRequest Input { get; set; } = new();
    public ProductResponse? Existing { get; private set; }
    public List<SelectListItem> Categories { get; private set; } = [];
    public List<SelectListItem> Vendors { get; private set; } = [];
    private async Task LoadAsync(int? id, CancellationToken ct)
    {
        if (id.HasValue) Existing = await service.GetByIdAsync(id.Value, ct);
        Categories = await lookups.CategoriesAsync(ct);
        Vendors = await lookups.VendorsAsync(ct);
        if (Existing is not null)
        {
            if (!Categories.Any(x => x.Value == Existing.Category.Id.ToString()))
                Categories.Add(new SelectListItem(Existing.Category.Name + " (inactive)", Existing.Category.Id.ToString()));
            if (!Vendors.Any(x => x.Value == Existing.PrimaryVendor.Id.ToString()))
                Vendors.Add(new SelectListItem(Existing.PrimaryVendor.Name + " (inactive)", Existing.PrimaryVendor.Id.ToString()));
        }
    }
    public async Task OnGetAsync(int? id, CancellationToken ct)
    {
        await LoadAsync(id, ct);
        if (Existing is { } item) Input = new() { Name = item.Name, Sku = item.Sku, CategoryId = item.Category.Id, PrimaryVendorId = item.PrimaryVendor.Id, ReorderThreshold = item.ReorderThreshold, Cost = item.Cost, RetailPrice = item.RetailPrice };
    }
    public Task<IActionResult> OnPostAsync(int? id, CancellationToken ct) => SaveAsync(async () =>
    {
        var item = id.HasValue
            ? await service.UpdateAsync(id.Value, new UpdateProductRequest { Name = Input.Name, Sku = Input.Sku, CategoryId = Input.CategoryId, PrimaryVendorId = Input.PrimaryVendorId, ReorderThreshold = Input.ReorderThreshold, Cost = Input.Cost, RetailPrice = Input.RetailPrice }, ct)
            : await service.CreateAsync(Input, ct);
        return Saved("Product saved.", "/Products/Details", new { id = item.Id });
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
            return Saved("Product " + (active ? "reactivated." : "deactivated."), "/Products/Index");
        }, () => OnGetAsync(id, ct));
    }
}

