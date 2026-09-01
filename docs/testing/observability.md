# Observability and Monitoring Architecture

## Purpose

Observability answers:

> What is the running system doing, and why?

The project uses complementary mechanisms:

```text
ILogger structured logs
+ ASP.NET Core request/exception telemetry
+ health checks
+ Azure Monitor/Application Insights later
+ SQL-backed business AuditLog
```

## Logs, metrics, traces

### Logs
Discrete diagnostic events.

### Metrics
Numeric measurements over time, such as request count, failures, latency, CPU, or dependency duration.

### Traces
Correlated execution across a request and its dependencies.

Example:

```text
POST /api/restocks
  → RestockService
  → SQL query
  → SQL SaveChanges
```

## ILogger<T>

Application code uses the built-in `ILogger<T>` abstraction.

Development output may go to console/Visual Studio.

The application should not couple business code to a specific logging vendor.

## Structured logging

Prefer named message properties:

```text
Recorded restock {RestockId} from vendor {VendorId} with {ItemCount} items
```

This produces queryable fields rather than one opaque string.

## Levels

### Trace
Extremely detailed. Normally off in production.

### Debug
Developer diagnostics.

### Information
Normal meaningful application events.

### Warning
Unexpected but handled conditions worth operational attention.

### Error
Unexpected operation failure.

### Critical
Rare application/system-wide failure.

Do not classify every ordinary validation or expected 409 as Error.

## Exception ownership

Centralized exception handling normally logs unexpected exceptions.

Avoid logging the same exception in Service, Controller, and middleware.

## Sensitive data

Never log:

- passwords
- PasswordHash
- authentication cookies
- antiforgery tokens
- connection-string credentials
- access/refresh tokens if added later

Prefer UserId to email in technical logs.

Do not enable EF Core sensitive-data logging in production.

## AuditLog vs ILogger

### ILogger / OpenTelemetry
Technical diagnostics and operations.

### AuditLog table
Durable business accountability.

Examples:
- who changed Product 42?
- who recorded Restock 91?
- who changed a user's role?

Telemetry can be filtered/sampled/retained differently and therefore must not replace the business audit table.

## Health endpoints

### `/health`
Liveness:

> Is the ASP.NET Core process alive and answering?

### `/health/ready`
Readiness:

> Can the app perform core work, including reaching SQL Server?

Readiness should include ApplicationDbContext/database connectivity.

Responses expose only status, not credentials, stack traces, or connection strings.

## Why liveness and readiness differ

The web process may be alive while SQL Server is unavailable.

In that case:

```text
/health       = healthy
/health/ready = unhealthy
```

This distinction is useful to deployment/platform monitoring.

## Azure Monitor / Application Insights

When Azure deployment is implemented, use the Azure Monitor OpenTelemetry ASP.NET Core distribution.

Planned package:

`Azure.Monitor.OpenTelemetry.AspNetCore`

Production configuration comes from environment/Azure settings, not Git.

Expected telemetry includes:

- HTTP requests
- SQL/dependency calls
- exceptions
- traces/logs
- metrics

## OpenTelemetry

OpenTelemetry provides vendor-neutral instrumentation concepts.

Conceptually:

```text
ASP.NET Core
  → OpenTelemetry
  → Azure Monitor
  → Application Insights experiences
```

## Correlation

A failing request should be traceable across:

- HTTP request
- application log
- SQL dependency
- exception

Unexpected ProblemDetails may expose a safe trace identifier, but never a production stack trace.

## What we monitor first

- application availability
- database readiness
- failed request rate
- server exceptions
- slow endpoints
- slow/failing SQL dependencies
- authentication/lockout anomalies
- deployment regressions

No giant observability dashboard is required for the MVP.

## Business vs technical metrics

Technical:
- latency
- errors
- request rate
- SQL duration

Business:
- low-stock count
- restocks
- adjustments by reason

Business reporting remains grounded in application/database data rather than treating Application Insights as the business database.

## Interview summary

> I separate business auditing from technical observability. Durable AuditLog records in SQL answer who performed business actions, while ILogger and OpenTelemetry provide operational diagnostics. In Azure I use the Azure Monitor OpenTelemetry distribution so Application Insights can correlate HTTP requests, SQL dependencies, exceptions, logs, traces, and metrics. Separate liveness and readiness endpoints distinguish an alive process from one that cannot reach SQL Server, and structured logs intentionally avoid credentials and sensitive payloads.
