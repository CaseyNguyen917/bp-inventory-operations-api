# BP Franchise Inventory & Operations Management System
# Master Codex Implementation Specification

**Status:** AUTHORITATIVE IMPLEMENTATION CONTRACT  
**Target stack:** C# / ASP.NET Core Web API / .NET 10 / Entity Framework Core / SQL Server / ASP.NET Core Identity / Azure  
**Architecture:** Modular monolith  
**Primary deployment target:** Azure App Service + Azure SQL Database  
**Purpose:** This document is the single authoritative implementation specification for the coding phase.

---

# 0. Authority and Execution Rules

This file supersedes earlier planning notes whenever there is a conflict.

Earlier repository documentation remains useful for rationale and interview study, but **this file controls implementation**.

Codex must:

1. inspect the existing repository before changing anything;
2. preserve the existing solution/project if it already builds;
3. adapt namespaces/project names to the existing repository rather than recreating the solution unnecessarily;
4. implement the system in the milestone order defined near the end of this document;
5. implement **only the requested milestone** unless a tiny prerequisite is required to make that milestone compile;
6. run `dotnet build` after each implementation milestone;
7. run the relevant automated tests once tests exist;
8. report changed files and any design deviation;
9. never silently redesign the architecture;
10. stop and surface a genuine specification contradiction rather than inventing a new architecture.

If a small syntax/package detail has changed in the current .NET SDK, use the current supported equivalent while preserving the architectural intent.

---

# 1. Existing Repository Assumptions

The Visual Studio ASP.NET Core project and Git repository already exist.

Do **not**:

- recreate the Git repository;
- recreate the solution merely to use a preferred template;
- rename the repository without instruction;
- replace working project configuration with a new architecture template.

Logical structure should converge toward:

```text
repository-root/
├── README.md
├── docs/
│   └── codex-implementation-spec.md
├── src/
│   └── BPInventory.Api/             # logical name; preserve actual existing project name
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Controllers/
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   ├── Configurations/
│       │   └── Seed/
│       ├── Entities/
│       ├── Dtos/
│       ├── Services/
│       ├── Enums/
│       ├── Exceptions/
│       ├── Auth/
│       ├── Configuration/
│       └── Health/
└── tests/
    └── BPInventory.Api.Tests/
```

If the current repo has the API project at repository root rather than under `src/`, do not perform a disruptive move solely to match this tree. The **logical folders/layers matter more than physical cosmetic restructuring**.

---

# 2. Project Goal

Build a serious back-office inventory and operations management backend for a **single independently operated BP gas-station convenience store**.

The project must demonstrate:

- real business workflow modeling;
- relational database design;
- ASP.NET Core API development;
- EF Core persistence;
- transactional inventory operations;
- authentication and role-based authorization;
- testing;
- Azure deployment;
- observability;
- professional documentation.

The public/demo deployment uses **synthetic data only**.

This project is inspired by a real franchise workflow but is **not** a BP corporate system and must never claim official BP integration.

---

# 3. Explicit Scope

## 3.1 Included

- Product inventory CRUD
- Category management
- Vendor management
- current inventory quantity
- RestockEvent + RestockItem
- InventoryAdjustment
- low-stock report
- AuditLog
- ASP.NET Core Identity users
- Employee / Manager / Admin roles
- authentication
- authorization
- synthetic seed/demo data
- xUnit tests
- Azure App Service deployment
- Azure SQL Database
- Azure Monitor / Application Insights
- health checks
- professional README/docs
- GitHub Actions only if time remains after the core is complete

## 3.2 Excluded

Do not implement:

- POS/register integration
- payment processing
- fuel-pump integration
- fuel-tank monitoring
- barcode hardware scanning
- payroll
- employee scheduling
- accounting
- purchase orders
- vendor invoicing
- multi-store support
- mobile app
- AI forecasting
- microservices
- Kubernetes
- Terraform
- Redis
- message queues
- event sourcing
- CQRS framework
- MediatR
- a heavy frontend
- a custom OAuth authorization server

---

# 4. Non-Negotiable Architecture

Use a **modular monolith**.

Request path:

```text
HTTP Client
    ↓
ASP.NET Core middleware
    ↓
Controller
    ↓
Service
    ↓
ApplicationDbContext
    ↓
Entity Framework Core
    ↓
SQL Server / Azure SQL
```

Cross-cutting concerns:

- ASP.NET Core Identity
- authentication
- authorization
- validation
- ProblemDetails exception handling
- configuration
- structured logging
- health checks
- OpenAPI
- business AuditLog

## 4.1 Controllers

Controllers are thin HTTP boundaries.

Responsibilities:

- route definitions;
- model binding;
- request DTO acceptance;
- authorization policy attributes;
- invoking services;
- converting service result to HTTP response;
- `CreatedAtAction` / status codes.

Controllers must **not** contain:

- EF queries;
- inventory calculations;
- database transactions;
- significant business rules;
- manual authorization logic that belongs in policies/services.

## 4.2 Services

Services own business/application workflows.

Use interfaces for meaningful service boundaries:

- `IProductService`
- `ICategoryService`
- `IVendorService`
- `IRestockService`
- `IInventoryAdjustmentService`
- `IAuditService`
- `IAuthService` if useful for auth orchestration
- `IUserAdministrationService`
- `ICurrentUserContext`

Implementation classes are scoped.

Services may use `ApplicationDbContext` directly.

## 4.3 No Generic Repository

Do **not** add:

```text
IRepository<T>
GenericRepository<T>
IUnitOfWork
UnitOfWork
```

solely to wrap EF Core.

`ApplicationDbContext` / `DbSet<T>` are the persistence abstraction for this MVP.

## 4.4 No Clean-Architecture Project Explosion

Do not split the solution into Domain/Application/Infrastructure/Contracts projects unless this specification is explicitly revised.

One main API project is intentional.

## 4.5 No AutoMapper Initially

Use explicit manual DTO mapping.

Reason:

- small project;
- easier debugging;
- explicit contracts;
- fewer hidden mappings.

---

# 5. Dependency Injection and Lifetimes

Use ASP.NET Core built-in DI.

Expected lifetimes:

| Component | Lifetime |
|---|---|
| `ApplicationDbContext` | Scoped |
| business services | Scoped |
| `ICurrentUserContext` | Scoped |
| framework logging | framework-managed |
| configuration Options | normal Options pattern |

Do not create singleton services that depend directly on scoped `ApplicationDbContext`.

Controllers receive services through constructor injection.

---

# 6. Core Domain Model

The domain contains:

```text
ApplicationUser
Category
Vendor
Product
RestockEvent
RestockItem
InventoryAdjustment
AuditLog
```

Master data:

- ApplicationUser
- Category
- Vendor
- Product

Transactional/historical data:

- RestockEvent
- RestockItem
- InventoryAdjustment
- AuditLog

---

# 7. Entity Specifications

Use normal C# classes under `Entities/`.

Use UTC `DateTime` values for timestamps and name properties with `Utc` suffixes.

Do not introduce an `IClock` abstraction initially.

## 7.1 ApplicationUser

Inherit:

```text
ApplicationUser : IdentityUser
```

Additional fields:

| Property | Type | Required | Notes |
|---|---|---:|---|
| `DisplayName` | string | yes | max 120 |
| `IsActive` | bool | yes | default true |
| `CreatedAtUtc` | DateTime | yes | server-generated |

Identity's default string key is used.

Do not duplicate:

- Email
- UserName
- PasswordHash
- SecurityStamp
- lockout fields
- other built-in Identity properties

Email is the human login identifier.

Identity `UserName` should be set consistently from email.

---

## 7.2 Category

| Property | Type | Required | Notes |
|---|---|---:|---|
| `Id` | int | yes | PK |
| `Name` | string | yes | max 80, unique |
| `IsActive` | bool | yes | default true |
| `CreatedAtUtc` | DateTime | yes | |
| `UpdatedAtUtc` | DateTime | yes | |

Navigation:

```text
ICollection<Product> Products
```

---

## 7.3 Vendor

| Property | Type | Required | Notes |
|---|---|---:|---|
| `Id` | int | yes | PK |
| `Name` | string | yes | max 120, unique |
| `ContactName` | string? | no | max 120 |
| `Phone` | string? | no | max 30 |
| `Email` | string? | no | max 256 |
| `IsActive` | bool | yes | default true |
| `CreatedAtUtc` | DateTime | yes | |
| `UpdatedAtUtc` | DateTime | yes | |

Navigations:

```text
ICollection<Product> Products
ICollection<RestockEvent> RestockEvents
```

---

## 7.4 Product

| Property | Type | Required | Notes |
|---|---|---:|---|
| `Id` | int | yes | PK |
| `Name` | string | yes | max 120 |
| `Sku` | string | yes | max 50, unique |
| `CategoryId` | int | yes | FK |
| `PrimaryVendorId` | int | yes | FK |
| `QuantityOnHand` | int | yes | >= 0 |
| `ReorderThreshold` | int | yes | >= 0 |
| `Cost` | decimal | yes | decimal(10,2), >= 0 |
| `RetailPrice` | decimal | yes | decimal(10,2), >= 0 |
| `IsActive` | bool | yes | default true |
| `CreatedAtUtc` | DateTime | yes | |
| `UpdatedAtUtc` | DateTime | yes | |

Navigations:

```text
Category Category
Vendor PrimaryVendor
ICollection<RestockItem> RestockItems
ICollection<InventoryAdjustment> InventoryAdjustments
```

### Critical Product rule

`QuantityOnHand` is **server-controlled inventory state**.

It may change only through:

- Restock workflow;
- InventoryAdjustment workflow;
- controlled synthetic seed initialization.

It must never be editable by general Product CRUD.

New Product creation always starts:

```text
QuantityOnHand = 0
```

---

## 7.5 RestockEvent

| Property | Type | Required | Notes |
|---|---|---:|---|
| `Id` | int | yes | PK |
| `VendorId` | int | yes | FK |
| `RecordedByUserId` | string | yes | Identity FK |
| `ReceivedAtUtc` | DateTime | yes | business event time |
| `Notes` | string? | no | max 1000 |
| `CreatedAtUtc` | DateTime | yes | server record creation |

Navigations:

```text
Vendor Vendor
ApplicationUser RecordedByUser
ICollection<RestockItem> Items
```

---

## 7.6 RestockItem

| Property | Type | Required | Notes |
|---|---|---:|---|
| `Id` | int | yes | PK |
| `RestockEventId` | int | yes | FK |
| `ProductId` | int | yes | FK |
| `QuantityReceived` | int | yes | > 0 |

Navigations:

```text
RestockEvent RestockEvent
Product Product
```

A single RestockEvent must not contain duplicate Product lines.

Enforce logically and with a unique composite index where practical:

```text
(RestockEventId, ProductId)
```

---

## 7.7 InventoryAdjustment

| Property | Type | Required | Notes |
|---|---|---:|---|
| `Id` | int | yes | PK |
| `ProductId` | int | yes | FK |
| `RecordedByUserId` | string | yes | Identity FK |
| `QuantityChange` | int | yes | non-zero, signed |
| `Reason` | AdjustmentReason | yes | store readable string |
| `Notes` | string? | no | max 1000 |
| `RecordedAtUtc` | DateTime | yes | server-generated |

Navigations:

```text
Product Product
ApplicationUser RecordedByUser
```

---

## 7.8 AuditLog

| Property | Type | Required | Notes |
|---|---|---:|---|
| `Id` | int | yes | PK |
| `UserId` | string? | no | nullable for system events |
| `Action` | string | yes | max 100 |
| `EntityType` | string | yes | max 100 |
| `EntityId` | string? | no | max 100 |
| `Details` | string? | no | nvarchar(max) |
| `TimestampUtc` | DateTime | yes | server-generated |

Navigation:

```text
ApplicationUser? User
```

AuditLog is intentionally generic.

Do not attempt a polymorphic FK to every possible audited entity.

---

# 8. Enums and Constants

## 8.1 AdjustmentReason

Create:

```text
Damage
Spoilage
Shrinkage
PhysicalCountCorrection
ManualCorrection
Other
```

Persist as a readable string using EF Core conversion.

Do not impose sign rules by reason in the MVP.

`QuantityChange` may be positive or negative, but resulting Product stock may never be negative.

## 8.2 Role Names

Use constants:

```text
Employee
Manager
Admin
```

Do not scatter string literals.

Example logical location:

```text
Auth/ApplicationRoles.cs
```

## 8.3 Authorization Policy Names

Use constants:

```text
EmployeeOrAbove
ManagerOrAbove
AdminOnly
```

## 8.4 Audit Action Names

Use stable constants such as:

```text
ProductCreated
ProductUpdated
ProductDeactivated
ProductReactivated

CategoryCreated
CategoryUpdated
CategoryDeactivated
CategoryReactivated

VendorCreated
VendorUpdated
VendorDeactivated
VendorReactivated

RestockRecorded
InventoryAdjusted

UserCreated
UserRoleChanged
UserDeactivated
UserReactivated
PasswordChanged
```

Do not log passwords or credential values in audit details.

---

# 9. Entity Framework Core / ApplicationDbContext

Create:

```text
ApplicationDbContext : IdentityDbContext<ApplicationUser>
```

The Identity tables and domain tables live in the same SQL Server database.

DbSets:

```text
Categories
Vendors
Products
RestockEvents
RestockItems
InventoryAdjustments
AuditLogs
```

Use separate `IEntityTypeConfiguration<T>` classes once configuration is non-trivial.

Expected:

```text
Data/Configurations/
├── CategoryConfiguration.cs
├── VendorConfiguration.cs
├── ProductConfiguration.cs
├── RestockEventConfiguration.cs
├── RestockItemConfiguration.cs
├── InventoryAdjustmentConfiguration.cs
├── AuditLogConfiguration.cs
└── ApplicationUserConfiguration.cs
```

`OnModelCreating` applies configurations from assembly and calls `base.OnModelCreating(builder)`.

---

# 10. SQL Constraints, Indexes, and Relationships

## 10.1 Unique Indexes

Required:

```text
Product.Sku
Category.Name
Vendor.Name
```

Identity must require unique Email.

Inactive records still reserve their unique key/name.

## 10.2 Check Constraints

Use SQL check constraints where supported by the EF version:

```text
Product.QuantityOnHand >= 0
Product.ReorderThreshold >= 0
Product.Cost >= 0
Product.RetailPrice >= 0
RestockItem.QuantityReceived > 0
InventoryAdjustment.QuantityChange <> 0
```

Application validation remains required too.

## 10.3 Delete Behavior

Use restrictive historical integrity.

```text
Category -> Products                  Restrict/NoAction
Vendor -> Products                    Restrict/NoAction
Vendor -> RestockEvents               Restrict/NoAction
Product -> RestockItems               Restrict/NoAction
Product -> InventoryAdjustments       Restrict/NoAction
ApplicationUser -> RestockEvents      Restrict/NoAction
ApplicationUser -> InventoryAdjustments Restrict/NoAction
ApplicationUser -> AuditLogs          Restrict/NoAction
```

Only:

```text
RestockEvent -> RestockItems
```

may cascade because line items have no independent meaning without their parent.

The public application still exposes no historical Restock delete endpoint.

## 10.4 Helpful Indexes

Include indexes appropriate for common filters:

- Product.CategoryId
- Product.PrimaryVendorId
- RestockEvent.VendorId
- RestockEvent.ReceivedAtUtc
- RestockItem.ProductId
- InventoryAdjustment.ProductId
- InventoryAdjustment.RecordedAtUtc
- AuditLog.TimestampUtc
- AuditLog.UserId
- AuditLog `(EntityType, EntityId)`

Do not over-index.

---

# 11. Master-Data Business Rules

## 11.1 Product

Create/update rules:

- Name required.
- SKU required.
- SKU unique.
- Category must exist and be active.
- Primary Vendor must exist and be active.
- ReorderThreshold >= 0.
- Cost >= 0.
- RetailPrice >= 0.
- create starts QuantityOnHand = 0.
- create starts IsActive = true.
- timestamps generated by server.

Update may edit only:

- Name
- Sku
- CategoryId
- PrimaryVendorId
- ReorderThreshold
- Cost
- RetailPrice

Update may not edit:

- QuantityOnHand
- IsActive
- CreatedAtUtc
- UpdatedAtUtc directly
- Id

Product deactivation:

- sets `IsActive = false`;
- preserves row/history;
- is idempotent;
- writes AuditLog.

Product reactivation:

- requires referenced Category and PrimaryVendor to currently be active;
- sets `IsActive = true`;
- writes AuditLog.

## 11.2 Category

Category can be deactivated only when **no active Product** currently references it.

Otherwise return Conflict.

Reason:

An active Product should not reference inactive required master data.

Reactivation is allowed if the row exists and uniqueness remains valid.

## 11.3 Vendor

Vendor can be deactivated only when **no active Product** uses it as PrimaryVendor.

Historical RestockEvents remain valid.

Reactivation is allowed if the row exists and uniqueness remains valid.

---

# 12. Inventory Model

Chosen design:

```text
Product.QuantityOnHand = current operational stock
Restock / Adjustment history = explanation of changes
```

Do not derive every read from full transaction history.

Do not update quantity outside controlled inventory workflows.

## 12.1 Low Stock Rule

A Product is low stock when:

```text
QuantityOnHand <= ReorderThreshold
```

Only active Products appear in the low-stock report.

`IsLowStock` is a derived API field, not a database column.

---

# 13. Restock Workflow

A Restock represents one incoming Vendor delivery with one or more Product lines.

Request must contain:

- VendorId
- ReceivedAtUtc
- optional Notes
- at least one line item
- each line has ProductId + QuantityReceived

Rules:

1. Vendor exists.
2. Vendor is active.
3. At least one item exists.
4. Every Product exists.
5. Every Product is active.
6. Every `QuantityReceived > 0`.
7. No duplicate ProductIds in one request.
8. For MVP, every restocked Product's `PrimaryVendorId` must equal the Restock VendorId.
9. Acting user comes from authenticated server context.
10. All changes are atomic.

The operation must atomically:

1. create RestockEvent;
2. create RestockItems;
3. increase every Product.QuantityOnHand;
4. create AuditLog;
5. commit.

If any line fails:

- no RestockEvent;
- no RestockItems;
- no Product quantity updates;
- no success AuditLog.

## 13.1 Transaction / Concurrency

Use an explicit EF Core transaction.

For inventory mutation workflows, use SQL Server transaction behavior strong enough to prevent lost updates.

Preferred MVP approach:

```text
Serializable isolation
```

for Restock and InventoryAdjustment transaction scopes.

This is acceptable for the low-concurrency single-store MVP and avoids adding rowversion/concurrency-token infrastructure now.

Do not add event sourcing or distributed transactions.

---

# 14. Inventory Adjustment Workflow

Request:

- ProductId
- QuantityChange
- Reason
- optional Notes

Server determines:

- RecordedByUserId
- RecordedAtUtc

Rules:

1. Product exists.
2. Product is active.
3. QuantityChange != 0.
4. Reason is a valid AdjustmentReason.
5. `newQuantity = currentQuantity + QuantityChange`.
6. `newQuantity >= 0`.
7. history + current quantity update atomically.
8. AuditLog written only on successful operation.

Use the same explicit transaction/concurrency strategy as Restock.

---

# 15. Historical Mutability

The public API does not expose ordinary update/delete endpoints for:

```text
RestockEvent
RestockItem
InventoryAdjustment
AuditLog
```

These represent historical facts/accountability.

If a future requirement needs correction, create an explicit correction/reversal workflow rather than arbitrary CRUD mutation.

---

# 16. API Conventions

Base prefix:

```text
/api
```

Infrastructure health endpoints do not use `/api`.

Routes:

- lowercase;
- plural resource nouns;
- hyphens for multi-word segments.

Examples:

```text
/api/products
/api/inventory-adjustments
/api/audit-logs
```

Use route constraints where helpful:

```text
/api/products/{id:int}
```

so `/api/products/low-stock` cannot conflict with `{id}`.

JSON:

- camelCase;
- ISO-8601 UTC timestamps.

No `/api/v1` prefix initially.

---

# 17. Shared API Types

## 17.1 PagedResponse<T>

Fields:

```text
Items
Page
PageSize
TotalCount
TotalPages
```

External JSON:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 0,
  "totalPages": 0
}
```

Rules:

- Page starts at 1.
- default PageSize = 25.
- maximum PageSize = 100.
- invalid Page/PageSize -> 400.

## 17.2 Summary DTOs

Use compact nested summary DTOs instead of serializing EF navigation graphs.

Examples:

```text
CategorySummaryResponse
VendorSummaryResponse
ProductSummaryResponse
UserSummaryResponse
```

---

# 18. Product DTO Contract

## 18.1 CreateProductRequest

Fields:

```text
Name
Sku
CategoryId
PrimaryVendorId
ReorderThreshold
Cost
RetailPrice
```

Validation:

- Name required, max 120.
- Sku required, max 50.
- CategoryId positive.
- PrimaryVendorId positive.
- ReorderThreshold >= 0.
- Cost >= 0.
- RetailPrice >= 0.

Must NOT include:

- Id
- QuantityOnHand
- IsActive
- timestamps

## 18.2 UpdateProductRequest

Same editable fields as Create.

No QuantityOnHand or IsActive.

## 18.3 ProductResponse

Fields:

```text
Id
Name
Sku
Category { Id, Name }
PrimaryVendor { Id, Name }
QuantityOnHand
ReorderThreshold
Cost
RetailPrice
IsLowStock
IsActive
CreatedAtUtc
UpdatedAtUtc
```

## 18.4 LowStockProductResponse

Fields:

```text
Id
Name
Sku
QuantityOnHand
ReorderThreshold
PrimaryVendor { Id, Name }
```

---

# 19. Category DTO Contract

## CreateCategoryRequest

```text
Name
```

Required, max 80.

## UpdateCategoryRequest

```text
Name
```

## CategoryResponse

```text
Id
Name
IsActive
CreatedAtUtc
UpdatedAtUtc
```

---

# 20. Vendor DTO Contract

## CreateVendorRequest / UpdateVendorRequest

```text
Name
ContactName?
Phone?
Email?
```

Validation:

- Name required, max 120.
- ContactName max 120.
- Phone max 30.
- Email valid format, max 256.

## VendorResponse

```text
Id
Name
ContactName
Phone
Email
IsActive
CreatedAtUtc
UpdatedAtUtc
```

---

# 21. Restock DTO Contract

## 21.1 CreateRestockRequest

```text
VendorId
ReceivedAtUtc
Notes?
Items
```

`Items` contains:

```text
ProductId
QuantityReceived
```

Notes max 1000.

ReceivedAtUtc is business event time supplied by client.

Do not accept `RecordedByUserId`.

## 21.2 RestockResponse

Fields:

```text
Id
Vendor { Id, Name }
RecordedBy { Id, DisplayName }
ReceivedAtUtc
Notes
CreatedAtUtc
Items [
  {
    Product { Id, Name, Sku }
    QuantityReceived
  }
]
```

## 21.3 RestockSummaryResponse

Fields:

```text
Id
Vendor { Id, Name }
ReceivedAtUtc
ItemCount
TotalUnitsReceived
```

---

# 22. Inventory Adjustment DTO Contract

## 22.1 CreateInventoryAdjustmentRequest

```text
ProductId
QuantityChange
Reason
Notes?
```

Do not accept:

- RecordedByUserId
- RecordedAtUtc

## 22.2 InventoryAdjustmentResponse

Fields:

```text
Id
Product { Id, Name, Sku }
QuantityChange
Reason
Notes
RecordedBy { Id, DisplayName }
RecordedAtUtc
```

Do not persist "PreviousQuantity" or "NewQuantity" solely for response convenience unless later required.

The current Product endpoint remains source of current quantity.

---

# 23. Audit DTO Contract

## AuditLogResponse

```text
Id
User { Id, DisplayName }?
Action
EntityType
EntityId
Details
TimestampUtc
```

AuditLog is read-only through public API.

---

# 24. Authentication/User DTO Contract

## 24.1 LoginRequest

```text
Email
Password
```

Never log either request body or password.

## 24.2 CurrentUserResponse

```text
Id
Email
DisplayName
Role
```

## 24.3 ChangePasswordRequest

```text
CurrentPassword
NewPassword
```

## 24.4 CreateUserRequest

Admin only.

```text
Email
DisplayName
InitialPassword
Role
```

## 24.5 ChangeUserRoleRequest

```text
Role
```

## 24.6 UserResponse

```text
Id
Email
DisplayName
Role
IsActive
CreatedAtUtc
```

Never expose:

- PasswordHash
- SecurityStamp
- ConcurrencyStamp
- auth cookie
- password reset/security tokens

---

# 25. Endpoint Contract

# 25.1 Products

```text
GET     /api/products
GET     /api/products/low-stock
GET     /api/products/{id:int}
POST    /api/products
PUT     /api/products/{id:int}
DELETE  /api/products/{id:int}
POST    /api/products/{id:int}/reactivate
```

Policies:

- GET/list/low-stock: EmployeeOrAbove
- POST/PUT/DELETE/reactivate: ManagerOrAbove

GET `/api/products` query parameters:

```text
page=1
pageSize=25
search
categoryId
vendorId
includeInactive=false
sortBy=name
sortDirection=asc
```

Allowed Product sort fields:

```text
name
sku
quantityOnHand
retailPrice
```

Search matches Name or SKU.

Low-stock query:

```text
page
pageSize
categoryId
vendorId
sortBy
sortDirection
```

Only active Products.

---

# 25.2 Categories

```text
GET     /api/categories
GET     /api/categories/{id:int}
POST    /api/categories
PUT     /api/categories/{id:int}
DELETE  /api/categories/{id:int}
POST    /api/categories/{id:int}/reactivate
```

Policies:

- GET: EmployeeOrAbove
- mutations: ManagerOrAbove

List query:

```text
page
pageSize
search
includeInactive=false
sortBy=name
sortDirection=asc
```

---

# 25.3 Vendors

```text
GET     /api/vendors
GET     /api/vendors/{id:int}
POST    /api/vendors
PUT     /api/vendors/{id:int}
DELETE  /api/vendors/{id:int}
POST    /api/vendors/{id:int}/reactivate
```

Policies:

- GET: EmployeeOrAbove
- mutations: ManagerOrAbove

List query mirrors Category.

---

# 25.4 Restocks

```text
GET  /api/restocks
GET  /api/restocks/{id:int}
POST /api/restocks
```

Policy:

EmployeeOrAbove.

List filters:

```text
page
pageSize
vendorId
productId
fromUtc
toUtc
```

Default ordering:

```text
ReceivedAtUtc DESC
```

No update/delete endpoints.

---

# 25.5 Inventory Adjustments

```text
GET  /api/inventory-adjustments
GET  /api/inventory-adjustments/{id:int}
POST /api/inventory-adjustments
```

Policy:

EmployeeOrAbove.

List filters:

```text
page
pageSize
productId
reason
fromUtc
toUtc
```

Default ordering:

```text
RecordedAtUtc DESC
```

No update/delete endpoints.

---

# 25.6 Audit Logs

```text
GET /api/audit-logs
GET /api/audit-logs/{id:int}
```

Policy:

ManagerOrAbove.

Filters:

```text
page
pageSize
userId
action
entityType
entityId
fromUtc
toUtc
```

Default:

```text
TimestampUtc DESC
```

---

# 25.7 Auth

```text
GET  /api/auth/antiforgery-token
POST /api/auth/login
GET  /api/auth/me
POST /api/auth/logout
POST /api/auth/change-password
```

Access:

| Endpoint | Access |
|---|---|
| antiforgery token | Anonymous |
| login | Anonymous + antiforgery |
| me | Authenticated |
| logout | Authenticated + antiforgery |
| change-password | Authenticated + antiforgery |

---

# 25.8 User Administration

```text
GET  /api/users
GET  /api/users/{id}
POST /api/users
PUT  /api/users/{id}/role
POST /api/users/{id}/deactivate
POST /api/users/{id}/reactivate
```

Policy:

AdminOnly.

List filters:

```text
page
pageSize
search
role
includeInactive
```

---

# 25.9 Health

```text
GET /health
GET /health/ready
```

Both anonymous.

`/health` = liveness only.

`/health/ready` = includes SQL Server/DbContext connectivity.

Never return secrets or stack traces.

Azure App Service Health Check uses:

```text
/health/ready
```

---

# 26. HTTP Status-Code Convention

Use:

| Situation | Status |
|---|---|
| successful GET | 200 |
| successful PUT | 200 |
| successful create POST | 201 |
| successful soft deactivate | 204 |
| successful logout/change password when no body | 204 |
| invalid DTO/static input | 400 |
| unauthenticated | 401 |
| authenticated but unauthorized | 403 |
| requested/referenced entity missing | 404 |
| valid-shaped request conflicts with state/business invariant | 409 |
| unexpected server error | 500 |

Use `Location` header / `CreatedAtAction` for created resources when practical.

Examples of 409:

- duplicate SKU;
- duplicate Category/Vendor name;
- negative-result InventoryAdjustment;
- trying to deactivate Category/Vendor while active Products reference it;
- reactivating Product while Category/Vendor is inactive;
- final active Admin demotion/deactivation;
- Restock Product does not belong to selected Primary Vendor.

Do not introduce 422 unless the specification is intentionally revised.

---

# 27. Validation Layers

## 27.1 DTO/Input Validation

Use ASP.NET Core `[ApiController]` + DataAnnotations initially.

Examples:

- required string;
- maximum length;
- valid email;
- non-negative numeric value;
- non-empty collection where expressible;
- page range.

## 27.2 Service Validation

Examples:

- resource existence;
- active state;
- uniqueness;
- final Admin protection;
- Restock Vendor/Product match;
- duplicate Restock lines;
- negative resulting inventory.

## 27.3 Database Validation

Use:

- FKs;
- unique indexes;
- check constraints.

This is defense in depth.

---

# 28. Exception Handling / ProblemDetails

Use centralized ASP.NET Core exception handling.

Use:

```text
AddProblemDetails
UseExceptionHandler
```

and a custom `IExceptionHandler` if useful.

Use a small exception set:

```text
NotFoundException
ConflictException
BusinessRuleException   # if separate from Conflict is useful
```

Mapping:

```text
NotFoundException -> 404
ConflictException -> 409
BusinessRuleException -> 409
unexpected Exception -> 500
```

Static model-validation errors remain 400 ValidationProblemDetails.

Do not add repetitive controller `try/catch`.

Unexpected 500 response:

- generic title/detail;
- optional safe trace ID;
- no stack trace;
- no SQL details;
- no secrets.

Unexpected exception should be logged once at centralized boundary.

---

# 29. Authentication Architecture

Use:

```text
ASP.NET Core Identity
+
Identity application cookie authentication
```

Do **not** implement a custom username/password JWT issuer.

Do not add:

```text
TokenService
JwtTokenGenerator
RefreshToken
```

for the MVP.

Future enterprise evolution may use Microsoft Entra ID / OAuth 2.0 / OpenID Connect with bearer access tokens.

---

# 30. Identity Configuration

Use:

```text
ApplicationUser : IdentityUser
```

Identity settings:

```text
RequireUniqueEmail = true
```

Password policy:

- minimum length 10;
- require lowercase;
- require uppercase;
- require digit;
- require non-alphanumeric.

Lockout:

- enabled;
- 5 failed attempts;
- 10-minute lockout.

No public self-registration.

No email-confirmation workflow.

No forgot-password email workflow.

No 2FA in MVP.

These are documented future enhancements.

---

# 31. Authentication Cookie

Recommended cookie configuration:

```text
Name = .BPInventory.Auth
HttpOnly = true
Secure = Always
SameSite = Strict
ExpireTimeSpan ≈ 8 hours
SlidingExpiration = false
```

MVP has no Remember Me option.

Development runs HTTPS as well.

---

# 32. Antiforgery / CSRF

Because browsers automatically send authentication cookies, unsafe methods require antiforgery protection.

Configure antiforgery request header:

```text
X-CSRF-TOKEN
```

Provide:

```text
GET /api/auth/antiforgery-token
```

using `IAntiforgery.GetAndStoreTokens`.

Return request token in JSON and store the antiforgery cookie.

Use ASP.NET Core automatic/global antiforgery validation for unsafe controller actions, or an equivalent centralized design.

Unsafe methods:

```text
POST
PUT
PATCH
DELETE
```

GET must not intentionally mutate business state.

Do not disable antiforgery merely to make OpenAPI UI easier.

---

# 33. Roles and Authorization Policies

Exactly one business role per user:

```text
Employee
Manager
Admin
```

Identity technically supports multiple roles, but application business logic enforces one.

Policies:

```text
EmployeeOrAbove = Employee OR Manager OR Admin
ManagerOrAbove = Manager OR Admin
AdminOnly = Admin
```

Do not assume role hierarchy exists automatically.

Authorization must be enforced by backend policies even if future UI hides buttons.

---

# 34. Current User Abstraction

Create scoped:

```text
ICurrentUserContext
```

Expected conceptual members:

```text
bool IsAuthenticated
string UserId
string? Email
string? DisplayName
IReadOnlyCollection<string> Roles
```

Implementation may use `IHttpContextAccessor`.

Business services must use `ICurrentUserContext` rather than reading raw HttpContext directly.

Operational request DTOs must not accept actor IDs.

---

# 35. User Administration Rules

Admin creates users.

CreateUser rules:

- unique email;
- valid DisplayName;
- valid initial password;
- Role must be Employee/Manager/Admin;
- new user starts active;
- exactly one role assigned.

Role change:

1. target exists;
2. role valid;
3. reject demotion of final active Admin;
4. remove prior business roles;
5. add new role;
6. update security stamp;
7. AuditLog.

Deactivate user:

- set IsActive false;
- reject self-deactivation through normal endpoint;
- reject deactivation of final active Admin;
- update security stamp;
- AuditLog.

Reactivate:

- set IsActive true;
- maintain exactly one valid role;
- update security state as needed;
- AuditLog.

Inactive users cannot log in.

Security-stamp validation interval should be around 5 minutes so deactivation/role changes propagate without a database query on every request.

---

# 36. Login Flow

Login:

1. antiforgery validation;
2. find user by email;
3. reject if missing/inactive with generic auth failure;
4. use Identity password verification / SignInManager;
5. apply lockout rules;
6. issue Identity auth cookie;
7. return CurrentUserResponse.

Do not reveal:

- whether an email exists;
- whether account inactive;
- exact password failure cause.

Technical logs may record safe internal diagnostic identifiers without passwords.

Logout uses Identity sign-out.

Change password uses UserManager/Identity APIs and never logs passwords.

---

# 37. CORS

Default:

Do not enable broad CORS.

Prefer same-origin frontend/API if a simple frontend is ever added.

Do not use:

```text
AllowAnyOrigin
```

for credentialed Production requests.

If a separate frontend is later added:

- allowed origins come from configuration;
- credentials explicitly configured;
- SameSite strategy revisited;
- antiforgery remains required.

CORS is not authentication or authorization.

---

# 38. Logging

Use:

```text
ILogger<T>
```

Do not require Serilog.

Use structured message templates with named properties.

Good events:

- Product created/deactivated/reactivated;
- Restock succeeded;
- Adjustment succeeded;
- User role/deactivation change;
- lockout/security operational event.

Do not log every getter/query.

Expected validation/conflict responses are not automatically Error logs.

Unexpected exception logging occurs centrally.

Never log:

- Password;
- PasswordHash;
- auth cookie;
- antiforgery token;
- DB credentials;
- connection-string password;
- access/refresh tokens;
- full authentication bodies.

Prefer UserId over email in routine technical logs.

---

# 39. AuditLog Requirements

AuditLog is separate from technical logs.

Required audited actions:

- Product create/update/deactivate/reactivate;
- Category create/update/deactivate/reactivate;
- Vendor create/update/deactivate/reactivate;
- Restock recorded;
- InventoryAdjustment recorded;
- User created;
- User role changed;
- User deactivated/reactivated;
- password changed (without credential detail).

Audit entry fields:

```text
UserId
Action
EntityType
EntityId
Details
TimestampUtc
```

Only create success audit entries after successful business operation.

When audit and business mutation belong to one operation, persist them in the same database transaction where practical.

---

# 40. OpenAPI

Use ASP.NET Core's supported built-in OpenAPI infrastructure.

Required:

- generated OpenAPI document;
- endpoint metadata;
- DTO schemas;
- documented response status codes where practical.

If the existing project already has a working interactive OpenAPI UI, preserve it.

If no UI exists, an interactive UI such as Scalar may be added during the API documentation/demo milestone if compatible with the current .NET SDK.

Do not redesign authentication just for an interactive UI.

---

# 41. Configuration

Use normal ASP.NET Core configuration.

Environments:

```text
Development
Testing
Production
```

Azure demo runs:

```text
ASPNETCORE_ENVIRONMENT=Production
```

Demo data is controlled independently:

```text
SeedData:Enabled=true
```

Do not invent a custom `Demo` hosting environment.

Configuration sources:

- `appsettings.json`
- `appsettings.Development.json`
- User Secrets in local Development
- environment variables
- Azure App Service configuration

Secrets never committed.

---

# 42. Strongly Typed Options

Use Options classes only for grouped settings that benefit from structure.

Expected:

```text
SeedDataOptions
```

Potential later:

```text
CorsOptions / FrontendOptions
```

Do not create Options classes for every scalar setting.

---

# 43. Seed Data

## 43.1 Roles

Ensure these roles idempotently:

```text
Employee
Manager
Admin
```

## 43.2 Demo Users

When `SeedData:Enabled`:

- Demo Employee;
- Demo Manager;
- Demo Admin.

Passwords supplied through configuration/User Secrets/Azure settings.

Never hard-code passwords.

Never log passwords.

## 43.3 Demo Business Data

Seed approximately:

- 4–6 Categories;
- 4 fictional Vendors;
- 20–30 Products;
- 6–10 historical Restocks;
- 8–12 Adjustments;
- multiple low-stock Products;
- at least one inactive Product.

All data synthetic.

Stable idempotency keys:

- role name;
- user email;
- Product SKU;
- Category name;
- Vendor name.

Repeated seeding must not duplicate data or automatically reset existing user passwords.

---

# 44. Suggested Synthetic Categories

Use ordinary convenience-store merchandise.

Examples:

```text
Beverages
Snacks
Candy
Automotive
Household Essentials
Personal Care
```

Avoid public demo data that implies regulated-product sales requirements unless there is a clear need.

---

# 45. Health Checks

Expose:

```text
GET /health
GET /health/ready
```

`/health`:

- process/liveness only.

`/health/ready`:

- application readiness;
- includes `ApplicationDbContext` / SQL connectivity.

No sensitive detail.

---

# 46. Testing Architecture

Test framework:

```text
xUnit
```

Test project:

```text
tests/BPInventory.Api.Tests
```

Use:

```text
Microsoft.AspNetCore.Mvc.Testing
```

for API integration tests.

Core persistence/business tests use a **real SQL Server test database**.

Do not use EF Core InMemory as primary proof of SQL Server behavior.

Do not mock DbSet LINQ queries as proof of SQL behavior.

Do not add a repository only to make tests easier.

---

# 47. Test Database

Use:

```text
BPInventory_Test
```

or an equally explicit test-only DB.

Connection:

```text
ConnectionStrings:TestConnection
```

Destructive reset helpers must verify they are pointed at an approved test DB before deleting data.

Apply real EF Core migrations.

Shared DB-mutating tests run serially unless deliberate isolation supports parallel execution.

---

# 48. Required High-Value Tests

The implementation is not complete without tests covering at least:

## Product

- valid create;
- quantity starts zero;
- duplicate SKU conflict;
- invalid Category/Vendor;
- list inactive filtering;
- search;
- category/vendor filters;
- pagination;
- update;
- soft deactivate/reactivate;
- low-stock threshold equality.

## Category/Vendor

- create;
- duplicate name;
- update;
- deactivation conflict when active Products reference;
- reactivation;
- history remains.

## Restock

- valid one-line;
- valid multi-line;
- correct quantity changes;
- correct actor;
- invalid Vendor;
- invalid Product;
- inactive Product/Vendor;
- Product's PrimaryVendor mismatch;
- duplicate line;
- zero/negative quantity;
- atomic rollback if any line invalid.

## Adjustment

- positive;
- negative;
- zero rejected;
- negative-result rejected;
- inactive Product rejected;
- atomic rollback.

## Authentication

- active user login;
- invalid password generic rejection;
- inactive user denied;
- lockout;
- logout;
- `/api/auth/me`;
- change password.

## Authorization

- anonymous protected -> 401;
- Employee Product create -> 403;
- Manager Product create -> allowed;
- Manager user administration -> 403;
- Admin user administration -> allowed;
- Employee AuditLog -> 403;
- Manager AuditLog -> allowed.

## Antiforgery

- unsafe authenticated request without token rejected;
- with valid token succeeds;
- GET does not require token.

## User Administration

- Admin creates user;
- duplicate email;
- invalid role;
- exactly one role;
- final Admin cannot be demoted/deactivated;
- self-deactivation rejected;
- deactivated user's historical references remain.

## Error Contract

- 400 ValidationProblemDetails;
- 404 ProblemDetails;
- 409 ProblemDetails;
- generic 500 no sensitive internals.

## Health

- `/health`;
- `/health/ready` healthy with SQL;
- no secret leakage.

---

# 49. Testing Style

Use behavior-oriented test names:

```text
RecordRestock_WithMultipleValidItems_IncreasesAllProductQuantities
RecordRestock_WhenAnyProductIsInvalid_DoesNotPersistPartialChanges
RecordAdjustment_WhenResultWouldBeNegative_IsRejected
Employee_WhenCreatingProduct_IsForbidden
```

Use Arrange / Act / Assert.

No arbitrary 100% coverage goal.

Prioritize business risk.

---

# 50. Azure Deployment Architecture

Target:

```text
Internet
   ↓ HTTPS
Azure App Service
   ↓ managed identity
Azure SQL Database

App Service
   ↓
Azure Monitor / Application Insights
```

Resources share one portfolio/demo Resource Group where practical.

The final region/SKU is selected at deployment time based on current cost/availability.

---

# 51. Azure SQL Authentication

Runtime App Service uses:

```text
system-assigned managed identity
```

No SQL password for normal runtime access.

Create contained Azure SQL user mapped to the App Service managed identity.

Runtime identity gets least-privilege data access.

Do not grant permanent `db_owner` solely to make migrations easy.

---

# 52. Azure SQL Networking

MVP:

- Azure SQL public network access enabled for selected networks only;
- allow App Service outbound IP addresses;
- allow temporary developer/admin IP when required;
- broad "Allow Azure services and resources to access this server" remains disabled.

Private Endpoint + VNet Integration are documented future hardening, not MVP implementation.

Do not add them unless instructed.

---

# 53. EF Core Migrations

Migrations are source-controlled.

Local:

```text
dotnet ef migrations add ...
dotnet ef database update
```

Testing:

apply real migrations to test DB.

Production/Azure:

use a reviewed EF Core migration bundle or explicit deployment migration step.

Do **not** unconditionally call:

```text
Database.Migrate()
```

during normal Production App Service startup.

Runtime identity and schema-deployment identity remain separate.

---

# 54. Azure Observability

Use:

```text
Azure.Monitor.OpenTelemetry.AspNetCore
```

when Azure deployment milestone is reached.

Configuration uses Azure environment/App Service settings.

Application Insights / Azure Monitor should capture:

- HTTP requests;
- dependencies;
- exceptions;
- traces/logs;
- metrics.

Business AuditLog remains SQL-backed.

---

# 55. Azure Cost Discipline

Cloud cost is an architecture requirement.

Use:

- one disposable portfolio resource group;
- smallest practical App Service plan;
- Azure SQL serverless/auto-pause candidate for intermittent demo workload;
- Cost Management budget alerts;
- reasonable telemetry volume;
- delete/scale down resources when not needed.

Do not add costly network infrastructure merely for résumé buzzwords.

---

# 56. CI/CD — Optional After Core Completion

GitHub Actions must not delay the working backend.

If implemented:

CI:

```text
checkout
setup .NET
restore
build
provision disposable SQL Server
apply migrations
run tests
```

Deployment:

```text
CI success
→ GitHub OIDC federation to Azure
→ migration deployment step
→ App Service deploy
→ /health/ready check
```

Do not store a long-lived Azure client password when OIDC federation is available.

CI test containers are test infrastructure only and do not imply Docker production deployment.

---

# 57. Coding Conventions

Use normal modern C# conventions.

- PascalCase: classes, methods, properties.
- camelCase: locals/parameters.
- `I` prefix for interfaces.
- async I/O methods end in `Async`.
- use `CancellationToken` on service/controller async operations where reasonable and pass to EF.
- use `await` for EF async calls.
- avoid `.Result` / `.Wait()`.
- use `decimal` for money.
- use nullable reference types if project has them enabled.
- avoid unnecessary `var` if explicit type materially improves learning/readability; otherwise normal style is acceptable.
- keep methods focused.
- avoid static global service locators.
- no business logic in entity property setters solely to appear "domain-driven."
- no reflection-heavy abstractions.
- no premature generic frameworks.

Use namespaces consistent with the existing project.

---

# 58. Query / EF Guidance

- project directly into DTOs where simple/read-heavy;
- use `AsNoTracking()` for read-only queries;
- use tracked entities for mutations;
- avoid loading full navigation graphs unnecessarily;
- use `Include` only when needed;
- avoid N+1 queries;
- validate allow-listed sort fields explicitly;
- paginate at SQL query level before materialization;
- use `AnyAsync` for existence checks;
- use `SingleOrDefaultAsync` / `FirstOrDefaultAsync` intentionally;
- do not call `ToList()` before applying filters/pagination.

---

# 59. Service Design Expectations

Suggested service method responsibilities:

## ProductService

- list/filter/search/sort/paginate;
- get by ID;
- create;
- update;
- deactivate;
- reactivate;
- low-stock query.

## CategoryService

- list/get;
- create/update;
- deactivate/reactivate;
- enforce active Product reference rule.

## VendorService

- list/get;
- create/update;
- deactivate/reactivate;
- enforce active Product reference rule.

## RestockService

- list/get detail;
- validate request;
- transaction;
- create RestockEvent/Items;
- update Products;
- audit.

## InventoryAdjustmentService

- list/get;
- validate;
- transaction;
- update Product;
- create history;
- audit.

## AuditService

Provide a small helper for creating AuditLog records and Manager/Admin query operations.

Do not call `SaveChangesAsync` independently inside AuditService when it needs to participate in a caller's transaction unless explicitly intended.

Allow caller to add audit entity to current DbContext/transaction.

## UserAdministrationService

- list/get users;
- create user;
- change role;
- deactivate/reactivate;
- final Admin rules;
- security stamp;
- audit.

---

# 60. API Controllers

Expected controllers:

```text
ProductsController
CategoriesController
VendorsController
RestocksController
InventoryAdjustmentsController
AuditLogsController
AuthController
UsersController
```

Health checks are mapped through health-check middleware/endpoints, not a bespoke business controller unless the framework requires it.

Use `[ApiController]`.

Use route:

```text
[Route("api/[controller]")]
```

only if it produces the exact intended route; for multi-word resources, explicit route strings are clearer.

Example:

```text
[Route("api/inventory-adjustments")]
```

Avoid action names in ordinary CRUD routes.

---

# 61. Data Seeding Architecture

Use a clear initializer under:

```text
Data/Seed/
```

Suggested components:

```text
SeedDataOptions
DatabaseSeeder
```

or similarly simple names.

Do not create a complex hosted-service framework unless necessary.

The seeder may run during startup after the schema already exists.

It must **not** perform schema migrations in Production.

System role seeding may run idempotently.

Demo business/user seeding only runs when `SeedData:Enabled`.

---

# 62. Production Data Safety

The public Azure deployment uses synthetic demo data only.

Never seed:

- real employee names/emails;
- private Vendor contacts;
- real operational inventory;
- customer/payment data;
- fuel data;
- actual credentials.

README wording must clearly state this is inspired by a real franchise workflow and the public deployment uses synthetic data.

---

# 63. README Expectations

Final README should contain:

1. project summary;
2. business problem;
3. actual implemented tech stack;
4. architecture diagram;
5. core features;
6. key engineering decisions;
7. ER diagram;
8. API/OpenAPI link or summary;
9. local setup;
10. testing instructions;
11. Azure architecture/live demo if available;
12. screenshots/demo;
13. intentional scope exclusions.

Do not claim unfinished technologies/features.

---

# 64. Canonical Demo Flow

The completed system must support:

1. Employee login.
2. Employee views low-stock Products.
3. Employee records multi-item Restock.
4. Product quantities visibly increase.
5. Employee records damaged-item Adjustment.
6. Quantity decreases.
7. Employee attempts Manager-only Product mutation and receives 403.
8. Manager logs in and successfully performs Product management.
9. Manager views AuditLog showing prior business events.
10. Admin views/manages user roles.
11. Azure `/health/ready` is healthy.
12. Application Insights shows request/dependency telemetry.

---

# 65. Required NuGet / Framework Capabilities

Use only what the project needs.

Expected capabilities/packages depending on existing template:

Production:

- ASP.NET Core Web API/controller support
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.EntityFrameworkCore.Design` for tooling
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- built-in ASP.NET Core OpenAPI support
- health-check EF/DbContext integration as needed
- `Azure.Monitor.OpenTelemetry.AspNetCore` only when Azure observability milestone is reached

Tests:

- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `Microsoft.AspNetCore.Mvc.Testing`

Do not add libraries solely because they are popular.

Do not add:

- AutoMapper;
- MediatR;
- FluentValidation unless specification is intentionally changed;
- Serilog;
- repository libraries;
- JWT libraries for custom token issuance.

Use DataAnnotations for initial DTO validation.

---

# 66. Security Review Checklist

Before calling auth complete:

- no plaintext passwords;
- Identity PasswordHasher/SignInManager/UserManager used;
- no passwords in logs;
- no public registration;
- secure cookie;
- HttpOnly;
- SameSite Strict;
- antiforgery header flow;
- Employee/Manager/Admin policies;
- actor identity server-derived;
- final Admin protected;
- inactive user login rejected;
- security stamp updated after role/deactivation;
- no broad CORS;
- generic failed-login response;
- 401/403 behavior tested.

---

# 67. Database Review Checklist

Before calling persistence complete:

- migrations apply cleanly;
- unique SKU;
- unique Category/Vendor name;
- money decimal(10,2);
- non-negative Product stock/threshold/cost/prices;
- positive RestockItem quantity;
- non-zero Adjustment;
- FKs correct;
- delete behaviors correct;
- RestockEvent→RestockItems cascade only;
- history not hard-deleted;
- indexes exist for common filters;
- enum stored readably;
- Identity tables integrated.

---

# 68. Inventory Integrity Checklist

Before calling inventory complete:

- Product create begins stock 0;
- Product update cannot edit stock;
- Restock only active Vendor/Product;
- Restock Product vendor matches PrimaryVendor;
- duplicate Restock lines rejected;
- Restock all-or-nothing;
- Adjustment non-zero;
- Adjustment resulting stock >= 0;
- Adjustment all-or-nothing;
- success AuditLog participates coherently;
- concurrency strategy prevents normal lost update;
- low-stock equality counts as low.

---

# 69. Implementation Milestones

Codex must proceed in this order.

Do **not** generate the entire application in one uncontrolled pass.

---

## Milestone 1 — Startup / Project Infrastructure

Implement only foundational structure.

Tasks:

- inspect existing solution/project;
- confirm .NET target;
- add required base NuGet packages;
- create logical folders;
- configure controllers;
- configure built-in OpenAPI;
- configure ProblemDetails base infrastructure;
- configure configuration binding placeholders;
- keep project compiling.

Do not implement domain features yet.

Acceptance:

```text
dotnet build
```

passes.

---

## Milestone 2 — Entities and Enums

Implement:

- ApplicationUser;
- Category;
- Vendor;
- Product;
- RestockEvent;
- RestockItem;
- InventoryAdjustment;
- AuditLog;
- AdjustmentReason;
- role/policy/audit constants.

No controllers/services yet.

Acceptance:

- builds;
- entity model matches this specification.

---

## Milestone 3 — DbContext and EF Configurations

Implement:

- ApplicationDbContext inheriting IdentityDbContext<ApplicationUser>;
- DbSets;
- IEntityTypeConfiguration classes;
- lengths/types;
- indexes;
- relationships;
- delete behaviors;
- check constraints;
- enum string conversion.

Acceptance:

- builds;
- model configuration matches spec.

---

## Milestone 4 — Initial Migration / Local Database

Create initial EF migration.

Review generated migration.

Apply to local SQL Server.

Acceptance:

- migration succeeds;
- expected domain + Identity tables exist;
- constraints/indexes present.

Do not add Production auto-migration.

---

## Milestone 5 — Category, Vendor, Product Vertical Slice

Implement:

- DTOs;
- services;
- controllers;
- pagination/filter/search/sorting;
- Product low-stock query;
- soft-deactivate/reactivate;
- validation/business rules;
- basic audit entries for mutations;
- centralized exception mapping sufficient for these features.

Acceptance:

- endpoints compile/run;
- Product stock cannot be set/changed through CRUD;
- Product creation stock 0;
- 400/404/409 semantics correct.

---

## Milestone 6 — Restock Workflow

Implement:

- Restock DTOs;
- service;
- controller;
- list/detail queries;
- full validation;
- Vendor/Product match;
- duplicate-line rejection;
- explicit Serializable transaction;
- inventory increments;
- AuditLog.

Acceptance:

- valid multi-line Restock works;
- invalid line causes zero partial state.

---

## Milestone 7 — Inventory Adjustment Workflow

Implement:

- DTOs;
- service;
- controller;
- list/detail;
- enum parsing/serialization;
- negative-stock prevention;
- explicit Serializable transaction;
- AuditLog.

Acceptance:

- positive/negative valid changes work;
- invalid negative final stock leaves all state unchanged.

---

## Milestone 8 — Audit Query Surface

Implement Manager-readable AuditLog list/detail API with filtering/pagination.

If auth is not yet implemented, service/query may be prepared but endpoint authorization finalizes later.

Acceptance:

- audit records are queryable;
- normal historical resources remain immutable.

---

## Milestone 9 — Identity / Authentication Foundation

Implement:

- Identity configuration;
- ApplicationUser integration;
- roles;
- secure cookie configuration;
- lockout/password policy;
- antiforgery header/token endpoint;
- CurrentUserContext;
- login/me/logout/change-password.

Acceptance:

- active user login;
- secure cookie;
- antiforgery required for unsafe methods;
- inactive user denied;
- no custom JWT service.

---

## Milestone 10 — Authorization Policies

Implement:

- EmployeeOrAbove;
- ManagerOrAbove;
- AdminOnly;
- apply policies to all controllers exactly per endpoint matrix.

Acceptance:

- 401/403 behavior correct;
- Employee cannot manage Product;
- Manager cannot manage users;
- Admin can.

---

## Milestone 11 — User Administration

Implement:

- User DTOs;
- list/get;
- create;
- role change;
- deactivate/reactivate;
- final active Admin rule;
- self-deactivation rule;
- security stamp updates;
- AuditLog.

Acceptance:

- exactly one role;
- last Admin protected;
- deactivated user blocked.

---

## Milestone 12 — Finalized Error Handling / Validation

Complete centralized ProblemDetails handling across all services.

Acceptance:

- consistent 400/401/403/404/409/500;
- no repetitive controller try/catch;
- unexpected 500 safe and logged once.

---

## Milestone 13 — Seed Data

Implement:

- role initialization;
- SeedDataOptions;
- demo users;
- synthetic Categories/Vendors/Products;
- synthetic Restock/Adjustment history;
- inactive/low-stock examples;
- idempotency;
- no hard-coded passwords.

Acceptance:

- running seed twice creates no duplicates;
- canonical demo workflow has usable data.

---

## Milestone 14 — Health Checks / Logging

Implement:

- `/health`;
- `/health/ready`;
- SQL readiness;
- structured ILogger calls at important workflows;
- no sensitive logging.

Acceptance:

- liveness works without DB dependency;
- readiness reflects SQL connectivity.

---

## Milestone 15 — Automated Test Project

Create xUnit project and test infrastructure.

Implement required high-value tests from this specification.

Use real SQL Server test database and real migrations.

Acceptance:

```text
dotnet test
```

passes.

Do not fake SQL behavior with EF InMemory.

---

## Milestone 16 — Azure-Specific Configuration

Prepare:

- Production configuration;
- managed-identity connection strategy;
- Azure Monitor OpenTelemetry;
- secure environment settings;
- no Production runtime migration;
- deployment docs/checklist.

Acceptance:

- local Development still works;
- Azure configuration contains no password-dependent runtime SQL design.

---

## Milestone 17 — Azure Deployment

Deploy:

- App Service;
- Azure SQL Database;
- managed identity;
- SQL firewall selected networks;
- migration deployment;
- Production seed flag;
- Application Insights;
- `/health/ready`.

Acceptance:

- deployed canonical demo workflow succeeds;
- runtime SQL connection uses managed identity;
- Application Insights receives telemetry.

---

## Milestone 18 — CI/CD (Only If Time)

Implement GitHub Actions:

- restore/build;
- SQL integration test dependency;
- migrations;
- tests;
- optional Azure deployment via OIDC.

Acceptance:

- no long-lived Azure password;
- workflow fails on build/test/deploy error.

Skip this milestone if it threatens backend completion/polish.

---

## Milestone 19 — README / Demo Packaging

Generate final README based only on completed functionality.

Include:

- diagrams;
- API docs;
- run instructions;
- tests;
- Azure architecture;
- intentional scope.

Acceptance:

- reviewer can understand project in under two minutes;
- no false claims.

---

# 70. Milestone Execution Protocol for Codex

For every coding milestone:

1. inspect current repository state;
2. state which files will change;
3. implement only the milestone;
4. keep code straightforward and explainable;
5. run build;
6. run relevant tests if available;
7. fix compilation/test failures before declaring milestone complete;
8. summarize:
   - files added/changed;
   - what was implemented;
   - any deviation from spec;
   - exact command/result used to verify.

Do not leave knowingly broken scaffolding for later unless the milestone explicitly requires a placeholder.

---

# 71. Explicit "DO NOT INVENT" Rules

Codex must not independently add or switch to:

```text
Spring Boot
Java backend
AWS deployment
PostgreSQL
MongoDB
Cosmos DB
Minimal APIs instead of controller architecture
Clean Architecture multi-project split
generic repository
custom UnitOfWork
MediatR
CQRS
AutoMapper
FluentValidation
JWT TokenService
refresh tokens
OAuth authorization server
Redis
RabbitMQ
Kafka
event sourcing
microservices
Docker production deployment
Kubernetes
Terraform
React
Angular
mobile app
multi-store support
fuel integration
POS integration
payment processing
AI forecasting
```

unless explicitly instructed by the user in a later architecture revision.

---

# 72. Definition of a Strong Finished Version

The project is considered strong and résumé-ready when it has:

- working GitHub repository;
- professional docs;
- relational ER model;
- ASP.NET Core controller API;
- SQL Server/Azure SQL;
- Product CRUD;
- Categories/Vendors;
- Restocks;
- Inventory Adjustments;
- low-stock report;
- AuditLog;
- Identity login;
- RBAC;
- synthetic seed users/data;
- xUnit tests for critical rules;
- Azure App Service deployment;
- managed-identity Azure SQL connection;
- health checks;
- Application Insights/observability;
- OpenAPI;
- professional README;
- explainable architectural tradeoffs.

GitHub Actions is a bonus, not a requirement for the minimum strong finish.

---

# 73. Interview-Level Architectural Summary

The implementation should support this explanation:

> I designed the project as a modular ASP.NET Core monolith because the domain and deployment requirements did not justify distributed-system complexity. Controllers are thin HTTP boundaries, scoped services implement business workflows, and EF Core's scoped ApplicationDbContext handles SQL Server persistence directly rather than being wrapped in a generic repository. API DTOs are separate from entities to prevent over-posting and keep persistence concerns out of the HTTP contract.
>
> Inventory uses a current-state-plus-history model: Product stores QuantityOnHand for fast operational reads, while Restocks and InventoryAdjustments explain every normal stock mutation. Those workflows update history and current stock in explicit database transactions, so a failed multi-item restock cannot partially modify inventory.
>
> Authentication uses ASP.NET Core Identity and secure cookie authentication because the application is an internal browser-oriented system. Authorization is expressed through EmployeeOrAbove, ManagerOrAbove, and AdminOnly policies, and services derive actor identity from the authenticated server context rather than trusting user IDs from request bodies.
>
> Core persistence tests run against SQL Server rather than EF's InMemory provider, and API integration tests exercise the real ASP.NET Core pipeline. In Azure, the API runs on App Service and uses a system-assigned managed identity for passwordless Azure SQL access. Schema migrations use a separate deployment identity, while Azure Monitor/Application Insights provides technical observability and the SQL AuditLog preserves business accountability.

---

# 74. Final Instruction to Codex

This specification represents deliberate design decisions made before implementation.

Prefer:

- correctness;
- clarity;
- business traceability;
- testability;
- conventional ASP.NET Core patterns;
- limited scope;
- code the project owner can explain deeply.

Do not optimize for:

- maximum number of patterns;
- maximum number of libraries;
- résumé buzzword density;
- abstraction for abstraction's sake.

When implementation begins, start with **Milestone 1 only** unless the user explicitly requests a later milestone.

