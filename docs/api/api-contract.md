# REST API Contract

## 1. Purpose

This document defines the MVP HTTP API contract for the BP Franchise Inventory & Operations Management System.

The contract is designed before controller implementation so that routes, HTTP semantics, request DTOs, response DTOs, validation behavior, and error responses are deliberate rather than invented during coding.

Base application prefix:

`/api`

Infrastructure endpoints such as `/health` do not use the `/api` prefix.

---

## 2. API Design Principles

The API will:

- use plural resource nouns in routes
- use HTTP methods according to operation semantics
- use explicit request and response DTOs
- never expose EF Core entities directly
- use JSON with camelCase property names
- use ISO-8601 UTC timestamps
- use consistent ProblemDetails-style errors
- use pagination for potentially growing collections
- keep transactional history immutable through the public API
- avoid allowing general Product updates to modify QuantityOnHand
- derive the acting user from authentication rather than trusting user IDs supplied by clients

The API is an internal/business application API, not a public internet platform API.

API versioning is intentionally deferred for the MVP.

---

# 3. Product Endpoints

## GET /api/products

Retrieve a paginated collection of products.

### Query parameters

| Parameter | Type | Default | Description |
|---|---|---:|---|
| page | int | 1 | 1-based page number |
| pageSize | int | 25 | Number of records; maximum 100 |
| search | string | null | Case-insensitive Name/SKU search |
| categoryId | int | null | Filter by Category |
| vendorId | int | null | Filter by primary Vendor |
| includeInactive | bool | false | Include deactivated products |
| sortBy | string | name | Allowed: name, sku, quantityOnHand, retailPrice |
| sortDirection | string | asc | Allowed: asc, desc |

### Response

`200 OK`

Body: `PagedResponse<ProductResponse>`

---

## GET /api/products/{id}

Retrieve one Product.

### Responses

- `200 OK` with `ProductResponse`
- `404 Not Found` if no Product with the ID exists

Inactive Products remain retrievable by ID for historical/management purposes.

---

## POST /api/products

Create a Product.

### Authorization target

Manager or Admin.

### Request

`CreateProductRequest`

### Important rule

`QuantityOnHand` is not accepted from the client.

A newly created Product begins with:

`QuantityOnHand = 0`

If an already-existing physical product needs opening stock entered, inventory is established through an explicit InventoryAdjustment.

### Responses

- `201 Created` with `ProductResponse`
- `400 Bad Request` for request validation errors
- `404 Not Found` if referenced Category or Vendor does not exist
- `409 Conflict` if SKU is already used or referenced master data is not in an allowed state

`Location` should point to `/api/products/{id}`.

---

## PUT /api/products/{id}

Replace/update the editable Product metadata.

### Authorization target

Manager or Admin.

### Request

`UpdateProductRequest`

Editable:

- name
- sku
- categoryId
- primaryVendorId
- reorderThreshold
- cost
- retailPrice

Not editable through this endpoint:

- id
- quantityOnHand
- isActive
- createdAtUtc
- updatedAtUtc

### Responses

- `200 OK` with updated `ProductResponse`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

## DELETE /api/products/{id}

Deactivate a Product.

This endpoint performs a soft delete:

`IsActive = false`

It does not physically delete the row.

### Authorization target

Manager or Admin.

### Responses

- `204 No Content`
- `404 Not Found`

Repeated deactivation should be safe and may return `204 No Content`.

---

## POST /api/products/{id}/reactivate

Reactivate a previously deactivated Product.

### Authorization target

Manager or Admin.

### Responses

- `200 OK` with `ProductResponse`
- `404 Not Found`
- `409 Conflict` if reactivation violates a business constraint

---

## GET /api/products/low-stock

Retrieve active products where:

`QuantityOnHand <= ReorderThreshold`

### Query parameters

| Parameter | Type | Default | Description |
|---|---|---:|---|
| page | int | 1 | 1-based page |
| pageSize | int | 25 | Maximum 100 |
| categoryId | int | null | Optional Category filter |
| vendorId | int | null | Optional Vendor filter |
| sortBy | string | quantityOnHand | Allowed low-stock sort fields |
| sortDirection | string | asc | asc or desc |

Only active Products are included.

### Response

`200 OK` with `PagedResponse<LowStockProductResponse>`

---

# 4. Category Endpoints

## GET /api/categories

Retrieve categories.

Query parameters:

- `page`
- `pageSize`
- `search`
- `includeInactive`
- `sortBy=name`
- `sortDirection=asc|desc`

Response:

- `200 OK` with `PagedResponse<CategoryResponse>`

---

## GET /api/categories/{id}

Responses:

- `200 OK`
- `404 Not Found`

---

## POST /api/categories

Manager/Admin.

Request:

`CreateCategoryRequest`

Responses:

- `201 Created`
- `400 Bad Request`
- `409 Conflict` for duplicate name

---

## PUT /api/categories/{id}

Manager/Admin.

Request:

`UpdateCategoryRequest`

Responses:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

## DELETE /api/categories/{id}

Soft-deactivate the Category.

Manager/Admin.

Responses:

- `204 No Content`
- `404 Not Found`
- `409 Conflict` if business rules prevent deactivation in the current state

---

## POST /api/categories/{id}/reactivate

Manager/Admin.

Responses:

- `200 OK`
- `404 Not Found`
- `409 Conflict`

---

# 5. Vendor Endpoints

## GET /api/vendors

Query parameters:

- `page`
- `pageSize`
- `search`
- `includeInactive`
- `sortBy=name`
- `sortDirection=asc|desc`

Response:

- `200 OK` with `PagedResponse<VendorResponse>`

---

## GET /api/vendors/{id}

Responses:

- `200 OK`
- `404 Not Found`

---

## POST /api/vendors

Manager/Admin.

Request:

`CreateVendorRequest`

Responses:

- `201 Created`
- `400 Bad Request`
- `409 Conflict` for duplicate vendor name

---

## PUT /api/vendors/{id}

Manager/Admin.

Request:

`UpdateVendorRequest`

Responses:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`

---

## DELETE /api/vendors/{id}

Soft-deactivate Vendor.

Manager/Admin.

Responses:

- `204 No Content`
- `404 Not Found`
- `409 Conflict` where business state prevents the operation

---

## POST /api/vendors/{id}/reactivate

Manager/Admin.

Responses:

- `200 OK`
- `404 Not Found`
- `409 Conflict`

---

# 6. Restock Endpoints

Restock history is append-oriented.

The public MVP API does not expose:

- PUT restock
- PATCH restock
- DELETE restock

If a historical mistake needs correction, a future explicit correction workflow is preferable to silently rewriting inventory history.

---

## GET /api/restocks

Retrieve paginated restock history.

### Query parameters

| Parameter | Type | Default | Description |
|---|---|---:|---|
| page | int | 1 | Page |
| pageSize | int | 25 | Maximum 100 |
| vendorId | int | null | Vendor filter |
| productId | int | null | Delivery contains Product |
| fromUtc | datetime | null | Inclusive lower date/time bound |
| toUtc | datetime | null | Inclusive upper date/time bound |

Default ordering:

`ReceivedAtUtc DESC`

### Response

`200 OK` with `PagedResponse<RestockSummaryResponse>`

---

## GET /api/restocks/{id}

Retrieve full restock detail including line items.

Responses:

- `200 OK` with `RestockResponse`
- `404 Not Found`

---

## POST /api/restocks

Record an incoming vendor delivery.

### Authorization target

Employee, Manager, or Admin.

### Request

`CreateRestockRequest`

The request contains:

- vendorId
- receivedAtUtc
- optional notes
- one or more line items

The request does not contain `recordedByUserId`.

The server derives the current user from the authenticated identity.

### Business operation

The service must atomically:

1. validate the Vendor
2. validate all Products
3. validate all quantities
4. create RestockEvent
5. create RestockItems
6. increase each Product.QuantityOnHand
7. create relevant audit information
8. persist the operation

### Responses

- `201 Created` with `RestockResponse`
- `400 Bad Request`
- `404 Not Found` for missing Vendor/Product
- `409 Conflict` for invalid current business state

`Location` points to `/api/restocks/{id}`.

---

# 7. Inventory Adjustment Endpoints

Inventory adjustments are append-oriented historical records.

The public MVP API does not expose update or delete operations for adjustments.

---

## GET /api/inventory-adjustments

### Query parameters

| Parameter | Type | Default | Description |
|---|---|---:|---|
| page | int | 1 | Page |
| pageSize | int | 25 | Maximum 100 |
| productId | int | null | Product filter |
| reason | string | null | Adjustment reason |
| fromUtc | datetime | null | Inclusive lower bound |
| toUtc | datetime | null | Inclusive upper bound |

Default ordering:

`RecordedAtUtc DESC`

Response:

`200 OK` with `PagedResponse<InventoryAdjustmentResponse>`

---

## GET /api/inventory-adjustments/{id}

Responses:

- `200 OK`
- `404 Not Found`

---

## POST /api/inventory-adjustments

Record a non-restock inventory change.

### Authorization target

Employee with permission, Manager, or Admin.

Exact employee authorization can be refined during the RBAC phase.

### Request

`CreateInventoryAdjustmentRequest`

Contains:

- productId
- quantityChange
- reason
- optional notes

Does not contain:

- userId
- recordedAtUtc

The server determines the acting user and recording timestamp.

### Business operation

The service must:

1. load Product
2. ensure QuantityChange is non-zero
3. calculate resulting stock
4. reject if resulting stock is negative
5. create InventoryAdjustment
6. update Product.QuantityOnHand
7. create audit information
8. persist both changes atomically

### Responses

- `201 Created` with `InventoryAdjustmentResponse`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict` if the adjustment conflicts with current inventory state

---

# 8. Audit Endpoints

Audit logs are read-only through the public MVP API.

---

## GET /api/audit-logs

### Authorization target

Manager or Admin.

### Query parameters

- page
- pageSize
- userId
- action
- entityType
- entityId
- fromUtc
- toUtc

Default order:

`TimestampUtc DESC`

Response:

`200 OK` with `PagedResponse<AuditLogResponse>`

---

## GET /api/audit-logs/{id}

Manager/Admin.

Responses:

- `200 OK`
- `404 Not Found`

---

# 9. Authentication / User Administration Endpoints

Exact authentication and administration contracts are intentionally deferred until the Authentication/RBAC design phase.

The final API will later include capabilities for:

- login/authentication
- current-user identity
- role assignment/administration as required

The current API contract MUST NOT invent its own user IDs in operational request bodies. Restock, adjustment, and audit actor identity will come from the authenticated user context.

---

# 10. Infrastructure Endpoint

## GET /health

Purpose:

- local smoke test
- deployment health probe
- Azure health monitoring later

Initial response:

`200 OK`

The health endpoint is infrastructure-oriented and does not use the `/api` prefix.

A deeper SQL Server readiness check may be added later, but the initial endpoint should remain simple.

---

# 11. Success Status-Code Conventions

| Operation | Status |
|---|---|
| Successful GET | 200 OK |
| Successful POST creating a resource | 201 Created |
| Successful PUT returning updated resource | 200 OK |
| Successful soft-deactivation | 204 No Content |
| Successful reactivation | 200 OK |

For `201 Created`, return the newly created response DTO and a Location header when practical.

---

# 12. Error Status-Code Conventions

| Status | Meaning in this API |
|---|---|
| 400 Bad Request | Invalid request shape/static validation |
| 401 Unauthorized | Authentication required or invalid |
| 403 Forbidden | Authenticated but insufficient permission |
| 404 Not Found | Requested/referenced resource does not exist |
| 409 Conflict | Request conflicts with current business/resource state |
| 500 Internal Server Error | Unexpected server failure |

Examples of `409 Conflict`:

- duplicate SKU
- duplicate unique Category/Vendor name
- adjustment would create negative stock
- attempting a workflow against deactivated master data where disallowed

The API avoids using many subtly different error codes unless they add clear value.

---

# 13. Transaction History Mutability Policy

The MVP intentionally exposes no general update/delete endpoints for:

- RestockEvent
- RestockItem
- InventoryAdjustment
- AuditLog

Reason:

These represent historical facts/accountability.

A future business requirement may introduce explicit correction/reversal operations, but arbitrary CRUD editing would undermine traceability.

---

# 14. Route Naming Rules

Use:

- lowercase route segments
- plural resource nouns
- hyphens for multi-word route segments

Examples:

- `/api/products`
- `/api/restocks`
- `/api/inventory-adjustments`
- `/api/audit-logs`
- `/api/products/low-stock`

Do not use verbs for ordinary CRUD routes.

Explicit domain operations such as `/reactivate` are allowed where they represent a clear state transition not cleanly expressed by the normal editable resource contract.

---

# 15. No API Version Prefix Initially

The MVP uses:

`/api/products`

rather than:

`/api/v1/products`

Reason:

- one internal client
- no published backward-compatibility commitment
- no existing version transition to manage

Versioning can be introduced before a breaking public contract change if real requirements justify it.

This is a scope decision, not a claim that API versioning is unimportant.
