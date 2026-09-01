using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BPInventoryOps.Api.Exceptions;

public sealed class ApiAntiforgeryFilter(IAntiforgery antiforgery)
    : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        HttpRequest request = context.HttpContext.Request;

        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method)
            || HttpMethods.IsTrace(request.Method))
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            ProblemDetails problemDetails = new()
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Antiforgery validation failed",
                Detail = "A valid antiforgery token is required for this request.",
                Instance = request.Path
            };
            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            ObjectResult result = new(problemDetails)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
            result.ContentTypes.Add("application/problem+json");
            context.Result = result;
        }
    }
}
