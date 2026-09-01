# Database Design

## 1. Purpose

This document defines the relational database design for the BP Franchise Inventory & Operations Management System MVP.

The schema is designed for:

- SQL Server
- Entity Framework Core
- A single store location
- Clear historical traceability
- Simple, explainable business rules
- A modular monolithic ASP.NET Core backend

The design intentionally favors correctness, maintainability, and interview explainability over unnecessary enterprise complexity.

---

## 2. Core Design Principles

### 2.1 Model business concepts, not screens

Tables represent durable business concepts and transactions rather than UI pages.

Examples:

- `Product` represents merchandise the store carries.
- `Category` represents product classification.
- `Vendor` represents a supplier.
- `RestockEvent` represents a delivery.
- `RestockItem` represents one product line within a delivery.
- `InventoryAdjustment` represents a non-restock inventory change.
- `AuditLog` represents an important application action.

### 2.2 Separate master data from transactional data

Master data changes relatively slowly:

- Product
- Category
- Vendor
- User

Transactional data represents things that happened:

- RestockEvent
- RestockItem
- InventoryAdjustment
- AuditLog

Transactional records should be preserved as historical evidence.

### 2.3 Preserve historical integrity

Products, categories, and vendors should normally be deactivated rather than hard deleted after they are referenced by historical records.

Transactional records should not be casually edited or deleted.

### 2.4 Keep current inventory easy to query

`Product.QuantityOnHand` stores the current inventory quantity.

Restocks and inventory adjustments preserve the history explaining why the quantity changed.

This is a deliberate tradeoff. The system does not derive current inventory by summing the entire transaction history on every read.

### 2.5 Use database constraints for important invariants

The application will validate business rules, but SQL Server should also protect important data integrity rules where practical.

Examples:

- Unique SKU
- Non-negative quantity
- Non-negative prices
- Positive restock quantities
- Required foreign keys

---

## 3. Entity Summary

The MVP relational model contains the following domain tables:

1. `Product`
2. `Category`
3. `Vendor`
4. `RestockEvent`
5. `RestockItem`
6. `InventoryAdjustment`
7. `AuditLog`

Authentication tables will later be supplied by ASP.NET Core Identity.

The application-level user entity will be referred to conceptually as `ApplicationUser`.

---

## 4. Product

### Purpose

Represents one merchandise item carried by the store.

### Proposed fields

| Field | Logical Type | Required | Notes |
|---|---|---:|---|
| Id | int | Yes | Primary key |
| Name | nvarchar(120) | Yes | Human-readable product name |
| Sku | nvarchar(50) | Yes | Unique business identifier |
| CategoryId | int | Yes | FK to Category |
| PrimaryVendorId | int | Yes | FK to Vendor |
| QuantityOnHand | int | Yes | Current stock quantity |
| ReorderThreshold | int | Yes | Low-stock threshold |
| Cost | decimal(10,2) | Yes | Current unit cost |
| RetailPrice | decimal(10,2) | Yes | Current selling price |
| IsActive | bit | Yes | Soft-delete/deactivation flag |
| CreatedAtUtc | datetime2 | Yes | Creation timestamp |
| UpdatedAtUtc | datetime2 | Yes | Last update timestamp |

### Constraints

- `Name` is required.
- `Sku` is required.
- `Sku` is unique.
- `QuantityOnHand >= 0`.
- `ReorderThreshold >= 0`.
- `Cost >= 0`.
- `RetailPrice >= 0`.
- `CategoryId` must reference an existing Category.
- `PrimaryVendorId` must reference an existing Vendor.

### Important design rule

`QuantityOnHand` should not normally be changed through a general Product update.

Inventory changes should flow through:

- `RestockEvent` / `RestockItem`
- `InventoryAdjustment`

This preserves traceability.

---

## 5. Category

### Purpose

Classifies products into business groupings such as Beverages, Snacks, Candy, Automotive, or Household.

### Proposed fields

| Field | Logical Type | Required | Notes |
|---|---|---:|---|
| Id | int | Yes | Primary key |
| Name | nvarchar(80) | Yes | Category name |
| IsActive | bit | Yes | Deactivation flag |
| CreatedAtUtc | datetime2 | Yes | Creation timestamp |
| UpdatedAtUtc | datetime2 | Yes | Last update timestamp |

### Constraints

- `Name` is required.
- `Name` should be unique for the MVP.

### Relationship

One Category can contain many Products.

A Product belongs to one Category.

`Category 1 -> many Product`

---

## 6. Vendor

### Purpose

Represents a supplier that provides merchandise to the store.

### Proposed fields

| Field | Logical Type | Required | Notes |
|---|---|---:|---|
| Id | int | Yes | Primary key |
| Name | nvarchar(120) | Yes | Vendor name |
| ContactName | nvarchar(120) | No | Optional representative/contact |
| Phone | nvarchar(30) | No | Optional phone |
| Email | nvarchar(256) | No | Optional email |
| IsActive | bit | Yes | Deactivation flag |
| CreatedAtUtc | datetime2 | Yes | Creation timestamp |
| UpdatedAtUtc | datetime2 | Yes | Last update timestamp |

### Constraints

- `Name` is required.
- Vendor name should be unique for the MVP.

### Relationships

One Vendor can be the primary vendor for many Products.

`Vendor 1 -> many Product`

One Vendor can be associated with many RestockEvents.

`Vendor 1 -> many RestockEvent`

### Scope decision

A Product has one primary Vendor in the MVP.

This avoids introducing a Product-Vendor many-to-many relationship with vendor-specific pricing, supplier priority, lead times, and other procurement details that are not required for the core inventory workflow.

---

## 7. RestockEvent

### Purpose

Represents one incoming delivery from a vendor.

A single delivery may contain multiple different products.

### Proposed fields

| Field | Logical Type | Required | Notes |
|---|---|---:|---|
| Id | int | Yes | Primary key |
| VendorId | int | Yes | FK to Vendor |
| RecordedByUserId | Identity user key | Yes | FK to ApplicationUser |
| ReceivedAtUtc | datetime2 | Yes | When delivery was received |
| Notes | nvarchar(1000) | No | Optional operational notes |
| CreatedAtUtc | datetime2 | Yes | When record was entered |

### Relationships

One Vendor can have many RestockEvents.

One ApplicationUser can record many RestockEvents.

One RestockEvent contains one or more RestockItems.

`RestockEvent 1 -> many RestockItem`

### Business rule

A RestockEvent must contain at least one RestockItem.

Recording the event should be treated as one logical operation.

If one line item fails, the application should avoid leaving a partially applied delivery.

This will later be implemented using a database transaction.

---

## 8. RestockItem

### Purpose

Represents one product line within a RestockEvent.

Example:

Restock #72:

- Coca-Cola 20 oz: +24
- Sprite 20 oz: +12
- Water 20 oz: +36

Each line is one RestockItem.

### Proposed fields

| Field | Logical Type | Required | Notes |
|---|---|---:|---|
| Id | int | Yes | Primary key |
| RestockEventId | int | Yes | FK to RestockEvent |
| ProductId | int | Yes | FK to Product |
| QuantityReceived | int | Yes | Number of units received |

### Constraints

- `QuantityReceived > 0`.
- `RestockEventId` must reference an existing RestockEvent.
- `ProductId` must reference an existing Product.

### Relationship pattern

RestockEvent and Product have a many-to-many business relationship across time:

- One RestockEvent can contain many Products.
- One Product can appear in many RestockEvents.

`RestockItem` resolves that relationship and stores relationship-specific data (`QuantityReceived`).

This is the same general pattern as:

- Order / OrderItem / Product
- Invoice / InvoiceLine / Product
- Enrollment / Student / Course

---

## 9. InventoryAdjustment

### Purpose

Represents an inventory change that is not a normal vendor restock.

Examples:

- Damage
- Spoilage
- Shrinkage
- Physical count correction
- Manual correction

### Proposed fields

| Field | Logical Type | Required | Notes |
|---|---|---:|---|
| Id | int | Yes | Primary key |
| ProductId | int | Yes | FK to Product |
| RecordedByUserId | Identity user key | Yes | FK to ApplicationUser |
| QuantityChange | int | Yes | Signed inventory change |
| Reason | nvarchar(40) | Yes | Controlled adjustment reason |
| Notes | nvarchar(1000) | No | Optional explanation |
| RecordedAtUtc | datetime2 | Yes | Time adjustment was recorded |

### Constraints

- `QuantityChange != 0`.
- `Reason` is required.
- Resulting inventory must not become negative.
- Product must exist.

### Signed quantity model

Examples:

- `+3` = increase inventory by 3
- `-4` = decrease inventory by 4

The application calculates:

`NewQuantity = CurrentQuantity + QuantityChange`

If `NewQuantity < 0`, the operation is rejected.

### Adjustment reason

The application will use a C# enum for supported reasons.

The recommended database representation is a readable string value such as:

- Damage
- Spoilage
- Shrinkage
- PhysicalCountCorrection
- ManualCorrection
- Other

---

## 10. AuditLog

### Purpose

Records important application actions for accountability.

AuditLog is broader than inventory history.

Inventory history answers:

> Why did stock change?

AuditLog answers:

> Who performed an important system action, what did they do, and when?

### Proposed fields

| Field | Logical Type | Required | Notes |
|---|---|---:|---|
| Id | int | Yes | Primary key |
| UserId | Identity user key | No | Acting user; nullable for system actions |
| Action | nvarchar(100) | Yes | Operation performed |
| EntityType | nvarchar(100) | Yes | Type of entity affected |
| EntityId | nvarchar(100) | No | Identifier represented as text |
| Details | nvarchar(max) | No | Optional structured/text detail |
| TimestampUtc | datetime2 | Yes | Time action occurred |

### Why EntityType + EntityId are not foreign keys

AuditLog may describe many different entity types:

- Product
- Vendor
- Category
- RestockEvent
- InventoryAdjustment
- User

A normal relational FK cannot point to multiple different target tables.

For the MVP, AuditLog therefore stores a generic entity type and identifier rather than a polymorphic foreign key.

This trades strict referential integrity for a much simpler general-purpose audit model.

---

## 11. ApplicationUser / ASP.NET Core Identity

Authentication and authorization will be implemented later with ASP.NET Core Identity.

Identity creates its own user, role, claim, token, and relationship tables.

The domain design only requires references to the authenticated user for actions such as:

- RestockEvent.RecordedByUserId
- InventoryAdjustment.RecordedByUserId
- AuditLog.UserId

The exact physical Identity schema is intentionally deferred until the authentication phase.

The project should avoid inventing a second independent `User` table that duplicates ASP.NET Core Identity.

---

## 12. Relationship Summary

### Category to Product

- Category: one
- Product: many
- FK: `Product.CategoryId`

### Vendor to Product

- Vendor: one
- Product: many
- FK: `Product.PrimaryVendorId`

### Vendor to RestockEvent

- Vendor: one
- RestockEvent: many
- FK: `RestockEvent.VendorId`

### ApplicationUser to RestockEvent

- ApplicationUser: one
- RestockEvent: many
- FK: `RestockEvent.RecordedByUserId`

### RestockEvent to RestockItem

- RestockEvent: one
- RestockItem: many
- FK: `RestockItem.RestockEventId`

### Product to RestockItem

- Product: one
- RestockItem: many
- FK: `RestockItem.ProductId`

### Product to InventoryAdjustment

- Product: one
- InventoryAdjustment: many
- FK: `InventoryAdjustment.ProductId`

### ApplicationUser to InventoryAdjustment

- ApplicationUser: one
- InventoryAdjustment: many
- FK: `InventoryAdjustment.RecordedByUserId`

### ApplicationUser to AuditLog

- ApplicationUser: one
- AuditLog: many
- FK: `AuditLog.UserId` where practical; nullable for system actions

---

## 13. Current Quantity: Stored Value vs Derived Ledger

A major design decision is whether current stock should be stored on Product or calculated from all historical events.

### Option A: derive current stock every time

Conceptually:

`CurrentStock = Sum(Restocks) + Sum(Adjustments)`

Advantages:

- Transaction history becomes the source of truth.
- Current quantity cannot drift independently if all changes are perfectly captured.

Disadvantages:

- More complicated queries.
- More expensive low-stock queries.
- Initial balances and future POS integration complicate the ledger.
- Harder for a beginner project to implement correctly.

### Option B: store Product.QuantityOnHand

Advantages:

- Simple and fast reads.
- Low-stock reporting is straightforward.
- Mirrors how many operational systems maintain current state plus history.
- Easier to explain and implement.

Disadvantage:

- Current quantity and historical records must be updated consistently.

### MVP decision

Use Option B.

`Product.QuantityOnHand` stores current state.

Restock and adjustment records preserve history.

The application must update the transaction record and QuantityOnHand atomically in one database transaction where appropriate.

---

## 14. Normalization

The schema targets approximately Third Normal Form (3NF) for the core domain.

### First Normal Form (1NF)

Values should be atomic and repeating groups should not be stored as repeated columns.

Bad design:

- Product1
- Quantity1
- Product2
- Quantity2
- Product3
- Quantity3

Instead:

- RestockEvent
- multiple RestockItem rows

### Second Normal Form (2NF)

Non-key attributes should depend on the whole logical key.

Using a dedicated RestockItem row prevents line-specific values such as QuantityReceived from being placed incorrectly on the RestockEvent.

### Third Normal Form (3NF)

Non-key attributes should describe the entity represented by that table rather than unrelated entities.

Examples:

Bad Product design:

- ProductId
- ProductName
- CategoryName
- CategoryDescription
- VendorPhone

This duplicates Category and Vendor information across products.

Normalized design:

- Product.CategoryId -> Category
- Product.PrimaryVendorId -> Vendor

Vendor and Category details live in their own tables.

---

## 15. Primary Keys

Each domain table uses a surrogate integer primary key for the MVP.

Example:

`Product.Id`

The SKU is a business key, but it is not used as the primary key.

### Why not use SKU as the primary key?

SKUs are business data and may eventually change.

Surrogate integer IDs:

- are stable
- make foreign keys compact
- separate database identity from mutable business identifiers

The database still enforces a unique constraint/index on SKU.

---

## 16. Foreign Keys

Foreign keys enforce valid relationships.

Example:

`Product.CategoryId -> Category.Id`

Without the FK, the database could contain:

`CategoryId = 9999`

even though Category 9999 does not exist.

Foreign keys protect referential integrity even if a bug bypasses application validation.

---

## 17. Delete Behavior

The project should prefer preserving referenced business data.

Recommended rules:

- Category -> Product: Restrict/NoAction
- Vendor -> Product: Restrict/NoAction
- Vendor -> RestockEvent: Restrict/NoAction
- Product -> RestockItem: Restrict/NoAction
- Product -> InventoryAdjustment: Restrict/NoAction
- RestockEvent -> RestockItem: may use cascade because RestockItem has no meaning without its parent, but the application should not expose normal deletion of historical restocks

Master entities should be deactivated through `IsActive` instead of deleted.

This avoids accidentally destroying historical relationships.

---

## 18. Indexing Strategy

Indexes speed reads but add storage and write cost.

The MVP should avoid speculative over-indexing.

Recommended initial indexes:

- Unique index on `Product.Sku`
- Unique index on `Category.Name`
- Unique index on `Vendor.Name`
- Indexes on foreign-key columns where useful:
  - Product.CategoryId
  - Product.PrimaryVendorId
  - RestockEvent.VendorId
  - RestockItem.RestockEventId
  - RestockItem.ProductId
  - InventoryAdjustment.ProductId

Entity Framework Core often creates indexes for foreign keys by convention, but generated migrations should be inspected rather than blindly assumed correct.

### Low-stock query

The low-stock condition compares two columns:

`QuantityOnHand <= ReorderThreshold`

Do not add a specialized index yet.

Measure actual query behavior before introducing computed columns or specialized indexing.

---

## 19. Decimal Types for Money

Do not use floating-point types such as `float` or `double` for currency.

Use decimal arithmetic.

Recommended SQL type:

`decimal(10,2)`

Examples:

- Cost
- RetailPrice

This avoids binary floating-point rounding behavior that is inappropriate for money.

---

## 20. Timestamps and UTC

Store application timestamps in UTC.

Examples:

- CreatedAtUtc
- UpdatedAtUtc
- ReceivedAtUtc
- RecordedAtUtc
- TimestampUtc

The UI or client can later convert UTC into local time for display.

This avoids ambiguity when systems are deployed to different environments.

---

## 21. Transaction Atomicity

A restock may update several products.

Example:

- Coke +24
- Sprite +12
- Water +36

The system should not commit only some lines if another line fails.

The desired behavior is:

`all succeed OR all fail`

This is the atomicity property of a database transaction.

The same principle applies when an InventoryAdjustment both:

1. creates an adjustment history record
2. updates Product.QuantityOnHand

These changes should succeed together.

---

## 22. Concurrency Note

Two employees could theoretically update the same product inventory at nearly the same time.

A naive read-modify-write sequence can create a lost update.

The MVP will first rely on transactional service logic and EF Core behavior.

Optimistic concurrency using a SQL Server `rowversion` column may be added if needed when implementation reaches inventory mutation logic.

It is intentionally not required in the initial schema because the project should not add complexity without a demonstrated need.

---

## 23. Deferred / Future Schema Extensions

Explicitly not part of the MVP schema:

- ProductVendor join table
- PurchaseOrder
- PurchaseOrderItem
- Invoice
- POS transaction
- Sale
- SaleItem
- StoreLocation
- FuelTank
- FuelPrice
- Barcode tables
- Forecasting tables

These can be discussed as future extensions without being implemented.

---

## 24. Interview Summary

A concise explanation of the database design:

> I started from the inventory workflows instead of designing tables first. I separated relatively stable master data such as Product, Category, and Vendor from transactional records such as RestockEvent and InventoryAdjustment. A restock has multiple RestockItems because one vendor delivery can contain multiple products. I store the current QuantityOnHand on Product for efficient operational reads while preserving restock and adjustment history for traceability, and those changes are intended to be committed atomically. I use soft deactivation for referenced master data, foreign keys for referential integrity, unique constraints for business identifiers such as SKU, decimal types for money, and UTC timestamps. The schema stays normalized and intentionally avoids unnecessary procurement, POS, fuel, and multi-store complexity.
