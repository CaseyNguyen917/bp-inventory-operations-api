using BPInventoryOps.Api.Dtos.Users;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace BPInventoryOps.Api.Pages.Users;
public sealed class EditModel(IUserAdministrationService service) : UiPageModel
{
    [BindProperty] public ChangeUserRoleRequest Input { get; set; } = new();
    public UserResponse Item { get; private set; } = null!;
    private async Task LoadAsync(string id, CancellationToken ct) => Item = await service.GetByIdAsync(id, ct);
    public async Task OnGetAsync(string id, CancellationToken ct)
    {
        await LoadAsync(id, ct);
        Input = new() { Role = Item.Role };
    }
    public Task<IActionResult> OnPostAsync(string id, CancellationToken ct) => SaveAsync(async () =>
    {
        await service.ChangeRoleAsync(id, Input, ct);
        return Saved("Role updated.", "/Index");
    }, () => LoadAsync(id, ct));
    public Task<IActionResult> OnPostDeactivateAsync(string id, CancellationToken ct) => ChangeStatusAsync(id, false, ct);
    public Task<IActionResult> OnPostReactivateAsync(string id, CancellationToken ct) => ChangeStatusAsync(id, true, ct);
    private Task<IActionResult> ChangeStatusAsync(string id, bool active, CancellationToken ct)
    {
        ModelState.Clear();
        return SaveAsync(async () =>
        {
            if (active) await service.ReactivateAsync(id, ct);
            else await service.DeactivateAsync(id, ct);
            return Saved("Account " + (active ? "reactivated." : "deactivated."), "/Users/Index");
        }, () => OnGetAsync(id, ct));
    }
}
