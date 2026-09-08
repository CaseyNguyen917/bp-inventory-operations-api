using BPInventoryOps.Api.Dtos.Auth;
using BPInventoryOps.Api.Pages.Shared;
using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BPInventoryOps.Api.Pages.Account;

public sealed class LoginModel(IAuthService auth) : UiPageModel
{
    [BindProperty] public LoginRequest Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();
    public Task<IActionResult> OnPostAsync(CancellationToken ct) => SaveAsync(async () =>
    {
        await auth.LoginAsync(Input, ct);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : Url.Page("/Index")!);
    });
}
