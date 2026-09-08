using BPInventoryOps.Api.Dtos.Users;
using BPInventoryOps.Api.Auth;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace BPInventoryOps.Api.Pages.Users;
public sealed class CreateModel(IUserAdministrationService service) : UiPageModel
{
    [BindProperty] public CreateUserRequest Input { get; set; } = new() { Role = ApplicationRoles.Employee };
    public Task<IActionResult> OnPostAsync(CancellationToken ct) => SaveAsync(async () =>
    {
        var item = await service.CreateAsync(Input, ct);
        return Saved("Team member created. Share the initial password securely.", "/Users/Edit", new { id = item.Id });
    });
}
