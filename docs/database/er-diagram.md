# ER Diagram

This document contains the conceptual MVP ER diagram for the BP Franchise Inventory & Operations Management System.

ASP.NET Core Identity will generate the physical authentication tables later. `ApplicationUser` is shown conceptually so domain relationships to the authenticated user are clear.

```mermaid
erDiagram
    CATEGORY ||--o{ PRODUCT : categorizes
    VENDOR ||--o{ PRODUCT : "primary supplier"
    VENDOR ||--o{ RESTOCK_EVENT : supplies
    APPLICATION_USER ||--o{ RESTOCK_EVENT : records
    RESTOCK_EVENT ||--|{ RESTOCK_ITEM : contains
    PRODUCT ||--o{ RESTOCK_ITEM : appears_in
    PRODUCT ||--o{ INVENTORY_ADJUSTMENT : receives
    APPLICATION_USER ||--o{ INVENTORY_ADJUSTMENT : records
    APPLICATION_USER ||--o{ AUDIT_LOG : performs

    CATEGORY {
        int Id PK
        nvarchar Name UK
        bit IsActive
        datetime2 CreatedAtUtc
        datetime2 UpdatedAtUtc
    }

    VENDOR {
        int Id PK
        nvarchar Name UK
        nvarchar ContactName
        nvarchar Phone
        nvarchar Email
        bit IsActive
        datetime2 CreatedAtUtc
        datetime2 UpdatedAtUtc
    }

    PRODUCT {
        int Id PK
        nvarchar Name
        nvarchar Sku UK
        int CategoryId FK
        int PrimaryVendorId FK
        int QuantityOnHand
        int ReorderThreshold
        decimal Cost
        decimal RetailPrice
        bit IsActive
        datetime2 CreatedAtUtc
        datetime2 UpdatedAtUtc
    }

    RESTOCK_EVENT {
        int Id PK
        int VendorId FK
        string RecordedByUserId FK
        datetime2 ReceivedAtUtc
        nvarchar Notes
        datetime2 CreatedAtUtc
    }

    RESTOCK_ITEM {
        int Id PK
        int RestockEventId FK
        int ProductId FK
        int QuantityReceived
    }

    INVENTORY_ADJUSTMENT {
        int Id PK
        int ProductId FK
        string RecordedByUserId FK
        int QuantityChange
        nvarchar Reason
        nvarchar Notes
        datetime2 RecordedAtUtc
    }

    APPLICATION_USER {
        string Id PK
        string UserName
        string Email
    }

    AUDIT_LOG {
        int Id PK
        string UserId FK
        nvarchar Action
        nvarchar EntityType
        nvarchar EntityId
        nvarchar Details
        datetime2 TimestampUtc
    }
```

## Cardinality Legend

- `||` = exactly one
- `o{` = zero or many
- `|{` = one or many

Examples:

`CATEGORY ||--o{ PRODUCT`

means:

- Each Product belongs to exactly one Category.
- A Category may contain zero or many Products.

`RESTOCK_EVENT ||--|{ RESTOCK_ITEM`

means:

- Each RestockItem belongs to exactly one RestockEvent.
- Each RestockEvent must contain one or more RestockItems.

## Key Design Pattern: RestockEvent + RestockItem

A delivery is modeled with a header-and-lines pattern.

Header:

`RestockEvent`

Lines:

`RestockItem`

This prevents repeating delivery-level data for every product and allows any number of products in one delivery.

The pattern is broadly reusable and resembles:

- Order + OrderItem
- Invoice + InvoiceLine
- PurchaseOrder + PurchaseOrderItem
