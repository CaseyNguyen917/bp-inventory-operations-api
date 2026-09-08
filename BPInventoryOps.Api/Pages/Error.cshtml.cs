using Microsoft.AspNetCore.Mvc.RazorPages;
namespace BPInventoryOps.Api.Pages;
public sealed class ErrorModel : PageModel
{
    public int Status { get; private set; }
    public string Heading { get; private set; } = "";
    public string Explanation { get; private set; } = "";
    public string Reference { get; private set; } = "";
    public void OnGet(int code = 500, string? reference = null)
    {
        // Preserve the original failed request's log reference across the redirect.
        Reference = reference is { Length: > 0 and <= 100 }
            && reference.All(c => char.IsAsciiLetterOrDigit(c) || c is ':' or '-' or '_')
            ? reference : HttpContext.TraceIdentifier;
        Status = code is 400 or 401 or 403 or 404 or 409 ? code : 500;
        Response.StatusCode = Status;
        Heading = Status == 404 ? "We couldn't find that record." : "We couldn't complete that request.";
        Explanation = Status == 400 ? "Check the filter or form values and try again." :
            Status == 404 ? "The link may be incorrect, or the record is no longer available." :
            "Return to the workspace and try again. If the issue continues, contact your administrator.";
    }
}
