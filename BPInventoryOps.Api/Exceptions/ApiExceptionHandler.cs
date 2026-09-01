using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BPInventoryOps.Api.Exceptions;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int status, string title, string detail) = exception switch
        {
            AuthenticationRequiredException => (
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                exception.Message),
            AuthenticationFailedException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                "Authentication failed."),
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message),
            ConflictException => (
                StatusCodes.Status409Conflict,
                "Request conflicts with current state",
                exception.Message),
            RequestValidationException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (
                StatusCodes.Status409Conflict,
                "Request conflicts with current state",
                "A record with the same unique value already exists."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing the request.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing request {TraceIdentifier}",
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = status;

        ProblemDetails problemDetails = new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        bool wasWritten = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        if (!wasWritten)
        {
            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken);
        }

        return true;
    }
}
