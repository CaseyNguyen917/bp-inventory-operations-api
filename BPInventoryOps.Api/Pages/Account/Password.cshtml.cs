using BPInventoryOps.Api.Dtos.Auth;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace BPInventoryOps.Api.Pages.Account;
public sealed class PasswordModel(IAuthService auth) : UiPageModel
{
    [BindProperty] public ChangePasswordRequest Input { get; set; } = new();
    public Task<IActionResult> OnPostAsync(CancellationToken ct) => SaveAsync(async () =>
    {
        await auth.ChangePasswordAsync(Input, ct);
        return Saved("Your password has been changed.", "/Index");
    });
}
