# API Examples

These examples illustrate the intended external contract. They are not implementation code.

# 1. Create Product

## Request

`POST /api/products`

```json
{
  "name": "Monster Energy 16 oz",
  "sku": "MONSTER16",
  "categoryId": 1,
  "primaryVendorId": 2,
  "reorderThreshold": 10,
  "cost": 1.75,
  "retailPrice": 3.49
}
```

## Successful response

`201 Created`

```json
{
  "id": 42,
  "name": "Monster Energy 16 oz",
  "sku": "MONSTER16",
  "category": {
    "id": 1,
    "name": "Beverages"
  },
  "primaryVendor": {
    "id": 2,
    "name": "Example Beverage Distributor"
  },
  "quantityOnHand": 0,
  "reorderThreshold": 10,
  "cost": 1.75,
  "retailPrice": 3.49,
  "isLowStock": true,
  "isActive": true,
  "createdAtUtc": "2026-08-31T18:30:00Z",
  "updatedAtUtc": "2026-08-31T18:30:00Z"
}
```

Notice that quantity starts at zero.

---

# 2. Enter Opening Physical Stock

After creating Product 42, suppose the employee counts 18 units physically present.

## Request

`POST /api/inventory-adjustments`

```json
{
  "productId": 42,
  "quantityChange": 18,
  "reason": "PhysicalCountCorrection",
  "notes": "Initial physical inventory count"
}
```

This preserves the explanation for the initial quantity rather than allowing Product creation to silently set stock.

---

# 3. Record Restock

## Request

`POST /api/restocks`

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

## Successful response

`201 Created`

The response contains the new RestockEvent and its lines.

Inventory for Product 42 increases by 24 and Product 43 by 12 within the same logical transaction.

---

# 4. Invalid Negative Inventory

Suppose Product 42 has 5 units.

## Request

`POST /api/inventory-adjustments`

```json
{
  "productId": 42,
  "quantityChange": -8,
  "reason": "Damage",
  "notes": "Count entered incorrectly"
}
```

## Response

`409 Conflict`

Representative body:

```json
{
  "title": "Inventory adjustment conflict",
  "status": 409,
  "detail": "The adjustment would reduce inventory below zero."
}
```

No InventoryAdjustment should be persisted and Product.QuantityOnHand remains unchanged.

---

# 5. Duplicate SKU

## Request

`POST /api/products`

with a SKU already used by another Product.

## Response

`409 Conflict`

This is not merely a malformed request; it conflicts with a uniqueness invariant/current persisted state.

---

# 6. Low Stock

Request:

`GET /api/products/low-stock?page=1&pageSize=25`

Response:

`200 OK`

```json
{
  "items": [
    {
      "id": 42,
      "name": "Monster Energy 16 oz",
      "sku": "MONSTER16",
      "quantityOnHand": 5,
      "reorderThreshold": 10,
      "primaryVendor": {
        "id": 2,
        "name": "Example Beverage Distributor"
      }
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 1,
  "totalPages": 1
}
```

---

# 7. Soft Delete

Request:

`DELETE /api/products/42`

Response:

`204 No Content`

Internally:

`Product.IsActive = false`

Historical RestockItems and InventoryAdjustments remain intact.

The API contract expresses removal from normal active use without exposing the database's physical deletion strategy.
