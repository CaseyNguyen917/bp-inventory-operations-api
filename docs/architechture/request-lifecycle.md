# Request Lifecycle and Cross-Cutting Concerns

## 1. Purpose

This document explains how an HTTP request moves through the BP Inventory backend.

Understanding this flow is important for debugging and interviews because ASP.NET Core is not simply "controller code." Requests pass through a configured processing pipeline.

---

## 2. High-Level Request Flow

```text
Client
  |
  v
Web Server / ASP.NET Core
  |
  v
Middleware Pipeline
  |
  +-- Exception handling
  +-- HTTPS redirection
  +-- Authentication (later)
  +-- Authorization (later)
  |
  v
Routing / Controller Endpoint
  |
  v
Controller
  |
  v
Application Service
  |
  v
ApplicationDbContext / EF Core
  |
  v
SQL Server
  |
  v
Response travels back through pipeline
  |
  v
Client
```

---

## 3. Program.cs

Modern ASP.NET Core applications use `Program.cs` to configure startup.

Two broad phases happen there.

### Service registration

Before the application is built:

```text
builder.Services...
```

This registers dependencies and framework services.

Examples later:

- controllers
- OpenAPI
- ProblemDetails
- ApplicationDbContext
- ProductService
- RestockService
- authentication
- authorization

### HTTP pipeline configuration

After:

```text
var app = builder.Build();
```

the application configures middleware and endpoints.

Examples:

- exception handler
- HTTPS redirection
- authentication
- authorization
- map controllers
- OpenAPI development endpoint

---

## 4. Dependency Injection During a Request

Suppose ProductsController requires IProductService.

ASP.NET Core creates/resolves the controller and supplies the registered service implementation.

ProductService may require ApplicationDbContext.

The dependency injection container supplies the scoped DbContext.

Conceptually:

```text
ProductsController
      |
      v
IProductService
      |
      v
ProductService
      |
      v
ApplicationDbContext
```

The controller does not manually instantiate these dependencies.

---

## 5. DbContext Lifetime

`ApplicationDbContext` is registered with `AddDbContext`.

Its normal lifetime is scoped.

For a conventional web request, this means one context instance is used within the request's dependency-injection scope.

A DbContext represents a short-lived unit of work.

It should not be stored globally or used as a singleton.

---

## 6. Example: Create Product

Conceptual HTTP request:

```text
POST /api/products
```

JSON body:

```text
{
  name,
  sku,
  categoryId,
  primaryVendorId,
  reorderThreshold,
  cost,
  retailPrice
}
```

Flow:

1. ASP.NET Core receives the HTTP request.
2. Routing selects ProductsController.
3. Model binding converts JSON into CreateProductRequest.
4. Basic DTO validation runs.
5. Controller calls IProductService.CreateAsync.
6. ProductService validates:
   - Category exists.
   - Vendor exists.
   - SKU is not already used.
7. ProductService creates Product with server-controlled fields.
8. DbContext tracks the entity.
9. SaveChangesAsync persists the new row.
10. Service returns ProductResponse.
11. Controller returns an appropriate HTTP response.
12. ASP.NET Core serializes the response to JSON.

---

## 7. Example: Invalid Inventory Adjustment

Request tries to change:

```text
Current Quantity = 5
QuantityChange = -8
```

Flow:

1. Controller receives a valid-shaped request.
2. InventoryAdjustmentService loads Product.
3. Service calculates:
   `5 + (-8) = -3`.
4. Domain rule fails.
5. Service produces/throws a known business failure.
6. Central exception handling maps the failure to a consistent HTTP ProblemDetails response.
7. No inventory change is committed.

The controller does not need its own repetitive try/catch block.

---

## 8. Authentication and Authorization Later

Authentication determines the current user.

Authorization evaluates whether the user is allowed to invoke the operation.

Conceptual future flow:

```text
Request with token
    |
Authentication
    |
User identity created
    |
Authorization
    |
Role/policy checked
    |
Controller
```

Example:

- Employee may GET products.
- Manager may POST products.
- Admin may manage roles.

These are cross-cutting endpoint-access concerns and should not be implemented as repeated manual role checks in every controller method.

---

## 9. Middleware Ordering

Middleware executes in an ordered pipeline.

Order can change behavior.

Example principle:

Exception handling should be early enough to catch failures from later components.

Authentication must run before authorization can make decisions about the authenticated user.

The project should use the normal ASP.NET Core pipeline rather than arbitrary ordering.

---

## 10. Logging vs Audit

During the same request, two different records may be relevant.

### Application log

Example:

```text
Failed to persist restock due to SQL timeout.
```

Purpose:

- diagnostics
- operations
- troubleshooting

### AuditLog

Example:

```text
Manager 42 deactivated Product 91.
```

Purpose:

- business accountability
- historical activity

These concerns should remain distinct.

---

## 11. Async/Await

Database and network operations should generally use asynchronous APIs.

Examples later:

- `ToListAsync`
- `SingleOrDefaultAsync`
- `SaveChangesAsync`

The purpose is not to make a single SQL query execute faster.

The purpose is to avoid blocking request threads while waiting for I/O, allowing the server to handle other work efficiently.

EF Core async operations should be awaited correctly.

---

## 12. Interview Summary

> An ASP.NET Core request first moves through the configured middleware pipeline, where cross-cutting concerns such as exception handling, authentication, and authorization are applied. Routing selects a controller action, model binding creates the request DTO, and the thin controller delegates the business operation to a scoped application service. The service enforces business rules and uses the request-scoped EF Core DbContext to query or mutate SQL Server data. Responses or application failures are then translated back into consistent HTTP responses, with centralized ProblemDetails handling for errors.
