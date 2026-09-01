# Logging Guidelines

## General principle

Log information that helps operate or diagnose the system. Do not log every method call.

## Recommended events

### Product
Information:
- Product created: ProductId, actor UserId
- Product deactivated/reactivated: ProductId, actor UserId

### Restock
Information:
- RestockId
- VendorId
- ItemCount
- actor UserId

Unexpected transaction/persistence failure:
- Error with exception object

### Inventory Adjustment
Information:
- AdjustmentId
- ProductId
- QuantityChange
- actor UserId

Expected business rejection is not automatically Error-level.

### Authentication/User administration
Useful events:
- lockout
- user deactivation
- role change

Never log passwords, cookies, or antiforgery tokens.

## Structured message templates

Prefer:

```text
Recorded restock {RestockId} for vendor {VendorId} with {ItemCount} items
```

over manually concatenated opaque strings.

## Expected vs unexpected failures

Expected:
- NotFound
- Conflict
- validation errors

These normally do not need Error-level stack traces.

Unexpected:
- unhandled database exception
- runtime failure

These are centrally logged with the exception.

## Development vs production

Development can use more Debug-level detail.

Production should favor signal over noise and normally use Information/Warning/Error categories appropriately.

Avoid production logging of full SQL parameter values or sensitive EF data.

## User identifiers

Prefer UserId in technical telemetry rather than routine email addresses.

## Trace identifiers

A production 500 response can expose a safe trace ID.

Support/debug flow:

```text
trace ID
→ search Application Insights
→ correlated request
→ SQL dependency
→ exception/logs
```

## User-controlled text

Do not blindly log full Notes/request bodies.

They may contain sensitive data and create unbounded noisy telemetry.

## Audit details

AuditLog entries may contain concise business descriptions but never credentials/security tokens.
