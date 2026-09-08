using BPInventoryOps.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BPInventoryOps.Api.Pages.Shared;

// Shared presentation behavior only; business rules remain in the existing services.
public abstract class UiPageModel : PageModel
{
    [TempData] public string? Notice { get; set; }

    protected async Task<IActionResult> SaveAsync(
        Func<Task<IActionResult>> save, Func<Task>? reload = null)
    {
        if (ModelState.IsValid)
        {
            try { return await save(); }
            catch (Exception error) when (error is ConflictException or RequestValidationException or NotFoundException or AuthenticationFailedException)
            {
                ModelState.AddModelError(string.Empty, error is AuthenticationFailedException
                    ? "Unable to sign in. Check your details and try again." : error.Message);
                Response.StatusCode = error is ConflictException ? 409 : 400;
            }
        }
        else { Response.StatusCode = 400; }
        if (reload is not null) await reload();
        return Page();
    }

    protected IActionResult Saved(string message, string page, object? routeValues = null)
    {
        Notice = message;
        return RedirectToPage(page, routeValues);
    }

    protected IActionResult? InvalidQuery()
    {
        return ModelState.IsValid ? null : RedirectToPage("/Error", new { code = 400 });
    }
}
