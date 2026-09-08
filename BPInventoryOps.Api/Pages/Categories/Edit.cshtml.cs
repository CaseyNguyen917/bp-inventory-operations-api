using BPInventoryOps.Api.Dtos.Categories;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Pages.Categories;
public sealed class EditModel(ICategoryService service) : UiPageModel
{
    [BindProperty] public CreateCategoryRequest Input { get; set; } = new();
    public CategoryResponse? Existing { get; private set; }
    
    private async Task LoadAsync(int? id, CancellationToken ct)
    {
        if (id.HasValue) Existing = await service.GetByIdAsync(id.Value, ct);
        
    }
    public async Task OnGetAsync(int? id, CancellationToken ct)
    {
        await LoadAsync(id, ct);
        if (Existing is { } item) Input = new() { Name = item.Name };
    }
    public Task<IActionResult> OnPostAsync(int? id, CancellationToken ct) => SaveAsync(async () =>
    {
        var item = id.HasValue
            ? await service.UpdateAsync(id.Value, new UpdateCategoryRequest { Name = Input.Name }, ct)
            : await service.CreateAsync(Input, ct);
        return Saved("Category saved.", "/Categories/Index", null);
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
            return Saved("Category " + (active ? "reactivated." : "deactivated."), "/Categories/Index");
        }, () => OnGetAsync(id, ct));
    }
}

