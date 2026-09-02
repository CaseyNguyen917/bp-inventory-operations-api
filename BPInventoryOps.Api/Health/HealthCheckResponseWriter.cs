using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BPInventoryOps.Api.Health;

public static class HealthCheckResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        return context.Response.WriteAsJsonAsync(
            new { status = report.Status.ToString() },
            context.RequestAborted);
    }
}
