# API DTO Contracts

## 1. General Rules

DTOs are API contracts and are separate from EF Core entities.

JSON uses camelCase.

Server-controlled fields are never accepted simply because they exist on an entity.

Operational actor identity is derived from the authenticated user rather than request bodies.

---

# 2. Shared DTOs

## PagedResponse<T>

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 125,
  "totalPages": 5
}
```

Fields:

- `items`
- `page`
- `pageSize`
- `totalCount`
- `totalPages`

Page numbering starts at 1.

---

## EntitySummaryResponse

Where a compact nested reference is useful, use a purpose-specific summary containing:

```json
{
  "id": 5,
  "name": "Beverages"
}
```

Do not serialize full navigation-property graphs.

---

# 3. Product DTOs

## CreateProductRequest

```json
{
  "name": "Coca-Cola 20 oz",
  "sku": "COKE20",
  "categoryId": 1,
  "primaryVendorId": 2,
  "reorderThreshold": 12,
  "cost": 1.10,
  "retailPrice": 2.49
}
```

Does NOT include:

- id
- quantityOnHand
- isActive
- timestamps

New Product inventory starts at zero.

---

## UpdateProductRequest

```json
{
  "name": "Coca-Cola 20 oz",
  "sku": "COKE20",
  "categoryId": 1,
  "primaryVendorId": 2,
  "reorderThreshold": 18,
  "cost": 1.15,
  "retailPrice": 2.69
}
```

This represents the complete editable metadata set used by PUT.

It does NOT directly modify QuantityOnHand or activation state.

---

## ProductResponse

```json
{
  "id": 42,
  "name": "Coca-Cola 20 oz",
  "sku": "COKE20",
  "category": {
    "id": 1,
    "name": "Beverages"
  },
  "primaryVendor": {
    "id": 2,
    "name": "Example Beverage Distributor"
  },
  "quantityOnHand": 34,
  "reorderThreshold": 12,
  "cost": 1.10,
  "retailPrice": 2.49,
  "isLowStock": false,
  "isActive": true,
  "createdAtUtc": "2026-08-31T18:30:00Z",
  "updatedAtUtc": "2026-08-31T18:30:00Z"
}
```

`isLowStock` is derived:

`quantityOnHand <= reorderThreshold`

It does not need its own database column.

---

## LowStockProductResponse

A compact response optimized for replenishment decisions.

```json
{
  "id": 42,
  "name": "Coca-Cola 20 oz",
  "sku": "COKE20",
  "quantityOnHand": 5,
  "reorderThreshold": 12,
  "primaryVendor": {
    "id": 2,
    "name": "Example Beverage Distributor"
  }
}
```

---

# 4. Category DTOs

## CreateCategoryRequest

```json
{
  "name": "Beverages"
}
```

## UpdateCategoryRequest

```json
{
  "name": "Cold Beverages"
}
```

## CategoryResponse

```json
{
  "id": 1,
  "name": "Beverages",
  "isActive": true,
  "createdAtUtc": "2026-08-31T18:30:00Z",
  "updatedAtUtc": "2026-08-31T18:30:00Z"
}
```

---

# 5. Vendor DTOs

## CreateVendorRequest

```json
{
  "name": "Example Beverage Distributor",
  "contactName": "Alex Smith",
  "phone": "555-0100",
  "email": "alex@example.com"
}
```

Optional:

- contactName
- phone
- email

Required:

- name

---

## UpdateVendorRequest

Same editable field set as CreateVendorRequest.

---

## VendorResponse

```json
{
  "id": 2,
  "name": "Example Beverage Distributor",
  "contactName": "Alex Smith",
  "phone": "555-0100",
  "email": "alex@example.com",
  "isActive": true,
  "createdAtUtc": "2026-08-31T18:30:00Z",
  "updatedAtUtc": "2026-08-31T18:30:00Z"
}
```

---

# 6. Restock DTOs

## CreateRestockRequest

```json
{
  "vendorId": 2,
  "receivedAtUtc": "2026-08-31T17:00:00Z",
  "notes": "Weekly beverage delivery",
  "items": [
    {
      "productId": 42,
      "quantityReceived": 24
    },
    {
      "productId": 43,
      "quantityReceived": 12
    }
  ]
}
```

Rules:

- vendorId required
- receivedAtUtc required
- at least one item
- each productId required
- each quantityReceived > 0
- duplicate Product entries within one Restock request should be rejected rather than silently merged

`recordedByUserId` is not accepted.

---

## RestockItemResponse

```json
{
  "product": {
    "id": 42,
    "name": "Coca-Cola 20 oz",
    "sku": "COKE20"
  },
  "quantityReceived": 24
}
```

---

## RestockResponse

```json
{
  "id": 91,
  "vendor": {
    "id": 2,
    "name": "Example Beverage Distributor"
  },
  "recordedBy": {
    "id": "identity-user-id",
    "displayName": "Employee User"
  },
  "receivedAtUtc": "2026-08-31T17:00:00Z",
  "notes": "Weekly beverage delivery",
  "createdAtUtc": "2026-08-31T17:05:10Z",
  "items": [
    {
      "product": {
        "id": 42,
        "name": "Coca-Cola 20 oz",
        "sku": "COKE20"
      },
      "quantityReceived": 24
    }
  ]
}
```

The exact user display field can be finalized with Identity design.

---

## RestockSummaryResponse

Used for list endpoints.

```json
{
  "id": 91,
  "vendor": {
    "id": 2,
    "name": "Example Beverage Distributor"
  },
  "receivedAtUtc": "2026-08-31T17:00:00Z",
  "itemCount": 8,
  "totalUnitsReceived": 144
}
```

`itemCount` and `totalUnitsReceived` are derived values.

---

# 7. Inventory Adjustment DTOs

## CreateInventoryAdjustmentRequest

```json
{
  "productId": 42,
  "quantityChange": -3,
  "reason": "Damage",
  "notes": "Three bottles damaged during unloading"
}
```

Does NOT include:

- recordedByUserId
- recordedAtUtc
- resultingQuantity

Rules:

- quantityChange != 0
- reason is a supported controlled value
- resulting inventory must remain >= 0

---

## InventoryAdjustmentResponse

```json
{
  "id": 125,
  "product": {
    "id": 42,
    "name": "Coca-Cola 20 oz",
    "sku": "COKE20"
  },
  "quantityChange": -3,
  "reason": "Damage",
  "notes": "Three bottles damaged during unloading",
  "recordedBy": {
    "id": "identity-user-id",
    "displayName": "Employee User"
  },
  "recordedAtUtc": "2026-08-31T17:20:00Z"
}
```

Current Product quantity is retrieved through Product APIs rather than being treated as a historical property of the adjustment record.

---

# 8. Audit DTO

## AuditLogResponse

```json
{
  "id": 4001,
  "user": {
    "id": "identity-user-id",
    "displayName": "Manager User"
  },
  "action": "ProductUpdated",
  "entityType": "Product",
  "entityId": "42",
  "details": "RetailPrice changed from 2.49 to 2.69",
  "timestampUtc": "2026-08-31T18:00:00Z"
}
```

Audit details may later become structured JSON if implementation needs more queryable change metadata.

---

# 9. Validation Ownership

## DTO validation

Examples:

- required string
- maximum string length
- non-negative price
- positive page/pageSize
- non-zero adjustment quantity
- non-empty restock items

## Service validation

Examples:

- referenced entity exists
- entity is active when workflow requires it
- SKU is unique
- resulting inventory is non-negative
- restock does not contain duplicate Product lines

## Database validation

Examples:

- foreign keys
- unique indexes
- check constraints

No single layer replaces the others.
