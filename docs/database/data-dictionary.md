# Data Dictionary

This document is the concise field-level reference for the MVP relational model.

## Product

| Column | Type | Null? | Key/Constraint | Meaning |
|---|---|---:|---|---|
| Id | int | No | PK | Internal stable identifier |
| Name | nvarchar(120) | No |  | Product display name |
| Sku | nvarchar(50) | No | UNIQUE | Store/business SKU |
| CategoryId | int | No | FK -> Category.Id | Product category |
| PrimaryVendorId | int | No | FK -> Vendor.Id | Default/primary supplier |
| QuantityOnHand | int | No | CHECK >= 0 | Current inventory |
| ReorderThreshold | int | No | CHECK >= 0 | Low-stock threshold |
| Cost | decimal(10,2) | No | CHECK >= 0 | Current unit cost |
| RetailPrice | decimal(10,2) | No | CHECK >= 0 | Current retail price |
| IsActive | bit | No | DEFAULT true | Product deactivation state |
| CreatedAtUtc | datetime2 | No |  | Creation time |
| UpdatedAtUtc | datetime2 | No |  | Last update time |

## Category

| Column | Type | Null? | Key/Constraint | Meaning |
|---|---|---:|---|---|
| Id | int | No | PK | Internal identifier |
| Name | nvarchar(80) | No | UNIQUE | Category name |
| IsActive | bit | No | DEFAULT true | Deactivation state |
| CreatedAtUtc | datetime2 | No |  | Creation time |
| UpdatedAtUtc | datetime2 | No |  | Last update time |

## Vendor

| Column | Type | Null? | Key/Constraint | Meaning |
|---|---|---:|---|---|
| Id | int | No | PK | Internal identifier |
| Name | nvarchar(120) | No | UNIQUE | Vendor name |
| ContactName | nvarchar(120) | Yes |  | Contact person |
| Phone | nvarchar(30) | Yes |  | Contact phone |
| Email | nvarchar(256) | Yes |  | Contact email |
| IsActive | bit | No | DEFAULT true | Deactivation state |
| CreatedAtUtc | datetime2 | No |  | Creation time |
| UpdatedAtUtc | datetime2 | No |  | Last update time |

## RestockEvent

| Column | Type | Null? | Key/Constraint | Meaning |
|---|---|---:|---|---|
| Id | int | No | PK | Restock identifier |
| VendorId | int | No | FK -> Vendor.Id | Delivery vendor |
| RecordedByUserId | Identity key | No | FK -> ApplicationUser | User who entered the restock |
| ReceivedAtUtc | datetime2 | No |  | Delivery receipt time |
| Notes | nvarchar(1000) | Yes |  | Optional notes |
| CreatedAtUtc | datetime2 | No |  | Record creation time |

## RestockItem

| Column | Type | Null? | Key/Constraint | Meaning |
|---|---|---:|---|---|
| Id | int | No | PK | Line identifier |
| RestockEventId | int | No | FK -> RestockEvent.Id | Parent delivery |
| ProductId | int | No | FK -> Product.Id | Product received |
| QuantityReceived | int | No | CHECK > 0 | Units received |

## InventoryAdjustment

| Column | Type | Null? | Key/Constraint | Meaning |
|---|---|---:|---|---|
| Id | int | No | PK | Adjustment identifier |
| ProductId | int | No | FK -> Product.Id | Affected product |
| RecordedByUserId | Identity key | No | FK -> ApplicationUser | User who entered adjustment |
| QuantityChange | int | No | CHECK != 0 | Signed inventory change |
| Reason | nvarchar(40) | No | Controlled value | Adjustment reason |
| Notes | nvarchar(1000) | Yes |  | Optional explanation |
| RecordedAtUtc | datetime2 | No |  | Adjustment time |

## AuditLog

| Column | Type | Null? | Key/Constraint | Meaning |
|---|---|---:|---|---|
| Id | int | No | PK | Audit record identifier |
| UserId | Identity key | Yes | FK where applicable | Acting user; nullable for system actions |
| Action | nvarchar(100) | No |  | Action performed |
| EntityType | nvarchar(100) | No |  | Affected entity type |
| EntityId | nvarchar(100) | Yes |  | Affected entity identifier |
| Details | nvarchar(max) | Yes |  | Additional context |
| TimestampUtc | datetime2 | No |  | Action time |

## Controlled Inventory Adjustment Reasons

Initial values:

- Damage
- Spoilage
- Shrinkage
- PhysicalCountCorrection
- ManualCorrection
- Other
