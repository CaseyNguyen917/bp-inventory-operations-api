using BPInventoryOps.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace BPInventoryOps.Api.Pages.Account;
public sealed class LogoutModel(IAuthService auth) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await auth.LogoutAsync();
        return RedirectToPage("/Account/Login");
    }
}
