# HTTP and API Conventions

## 1. Resource-Oriented Routes

Use nouns for resources:

Good:

- GET `/api/products`
- GET `/api/vendors/5`
- POST `/api/restocks`

Avoid:

- `/api/getProducts`
- `/api/createVendor`
- `/api/doRestock`

The HTTP method already expresses the general operation.

---

## 2. HTTP Methods

### GET

Read data.

GET must not intentionally modify business state.

### POST

Create a new resource or perform an explicit domain operation whose semantics are not a normal full resource replacement.

Examples:

- POST `/api/products`
- POST `/api/restocks`
- POST `/api/products/42/reactivate`

### PUT

Replace/update the complete editable representation for an existing resource.

Example:

- PUT `/api/products/42`

PUT is intended to be idempotent: repeating the same request should lead to the same resource state.

### DELETE

Remove the resource from normal active use.

For Product, Category, and Vendor this maps internally to soft deactivation rather than physical row deletion.

---

## 3. Why DELETE Can Mean Soft Delete

HTTP describes API semantics, not the physical SQL command used internally.

A caller saying:

`DELETE /api/products/42`

means the Product should no longer exist as an active business resource.

The persistence implementation can preserve the row using:

`IsActive = false`

This maintains historical integrity.

---

## 4. Idempotency

An operation is idempotent when applying the same request multiple times has the same intended end state as applying it once.

Examples:

- GET is idempotent.
- PUT is designed to be idempotent.
- DELETE/soft-deactivate is designed to be idempotent.
- POST create is generally not idempotent.

Example:

Calling DELETE Product 42 twice should not create two different deactivation effects.

---

## 5. Pagination

Collection endpoints that can grow use pagination.

Conventions:

- page numbering begins at 1
- default pageSize = 25
- maximum pageSize = 100

Invalid values such as page < 1 or pageSize < 1 should return 400.

The server clamps or rejects values over 100; implementation preference is to reject clearly invalid values rather than silently surprise clients.

Response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 0,
  "totalPages": 0
}
```

---

## 6. Filtering

Filters are expressed with query parameters.

Example:

`GET /api/products?categoryId=2&vendorId=5`

Filters should be explicit and allow-listed.

Do not expose an arbitrary SQL-like query language.

---

## 7. Search

Product search:

`GET /api/products?search=coke`

Search should initially match:

- Product.Name
- Product.Sku

Case-insensitively according to database/query semantics.

Advanced full-text search is out of scope.

---

## 8. Sorting

Use:

- `sortBy`
- `sortDirection`

Example:

`GET /api/products?sortBy=retailPrice&sortDirection=desc`

Allowed sort fields are explicitly defined.

Do not reflect arbitrary user-provided property/SQL column names into queries.

Default product sort:

- name ascending

Historical defaults:

- restocks: receivedAtUtc descending
- adjustments: recordedAtUtc descending
- audit logs: timestampUtc descending

---

## 9. UTC Dates

API timestamps use ISO-8601 UTC values.

Example:

`2026-08-31T17:20:00Z`

Date-range query parameters are also UTC:

- fromUtc
- toUtc

The backend stores and compares UTC.

Display conversion is a client concern.

---

## 10. JSON Naming

External JSON:

camelCase

Example:

```json
{
  "quantityOnHand": 12,
  "reorderThreshold": 8
}
```

C# properties use normal PascalCase:

```text
QuantityOnHand
ReorderThreshold
```

ASP.NET Core JSON serialization handles the external naming convention.

---

## 11. Error Format

Use ProblemDetails / ValidationProblemDetails-style responses.

Representative validation failure:

```json
{
  "type": "...",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": [
      "The name field is required."
    ]
  }
}
```

Representative business conflict:

```json
{
  "type": "...",
  "title": "Inventory adjustment conflict",
  "status": 409,
  "detail": "The adjustment would reduce inventory below zero."
}
```

Exact extensions can evolve, but errors must remain consistent.

---

## 12. 400 vs 404 vs 409

### 400 Bad Request

The request itself violates input rules.

Examples:

- missing name
- negative retail price
- zero quantityChange
- empty restock item list

### 404 Not Found

A specifically requested/referenced entity does not exist.

Examples:

- GET Product 999
- Create Product references Category 999
- Adjustment references Product 999

### 409 Conflict

The request is valid in shape but conflicts with current state or a uniqueness/business invariant.

Examples:

- duplicate SKU
- adjustment would make stock negative
- action against a deactivated resource where that workflow is prohibited

---

## 13. 401 vs 403

### 401 Unauthorized

Despite the HTTP name, this means the request lacks valid authentication.

Question:

> Who are you?

Not established.

### 403 Forbidden

Identity is established, but permission is insufficient.

Question:

> Are you allowed?

Answer is no.

Example:

Employee attempts Manager-only Product creation.

---

## 14. 201 Created

POST operations that create resources return:

`201 Created`

and, when practical:

`Location: /api/resource/{newId}`

The response body contains the created DTO.

This applies to:

- Product
- Category
- Vendor
- RestockEvent
- InventoryAdjustment

---

## 15. PUT vs PATCH Decision

The MVP uses PUT for Product/Category/Vendor metadata updates.

Why not PATCH initially?

PATCH supports partial changes but introduces an additional update contract and semantic complexity.

The editable representations are small enough that a complete update DTO is straightforward.

If partial updates become a real client need, PATCH can be introduced later.

---

## 16. Historical Resource Policy

Restocks and InventoryAdjustments are not ordinary mutable CRUD resources.

After creation they represent historical events.

The API therefore intentionally omits general PUT/DELETE endpoints.

This is a domain rule reflected at the HTTP boundary.

---

## 17. No API Versioning Yet

Versioning is deferred because:

- the API is initially internal
- only one application version exists
- no external client compatibility contract exists

Versioning should be added deliberately when the API needs to support incompatible contract generations, not as decorative routing.
