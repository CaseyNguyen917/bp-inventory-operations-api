# Project Structure

## 1. Solution Strategy

The MVP will use one primary ASP.NET Core Web API project.

Recommended logical repository structure:

```text
bp-inventory-operations-api/
|
|-- BPInventory.sln
|-- README.md
|
|-- docs/
|   |-- business-scope.md
|   |-- requirements.md
|   |-- database-design.md
|   |-- er-diagram.md
|   |-- data-dictionary.md
|   |-- architecture.md
|   |-- project-structure.md
|   `-- request-lifecycle.md
|
|-- src/
|   `-- BPInventory.Api/
|       |-- BPInventory.Api.csproj
|       |-- Program.cs
|       |-- appsettings.json
|       |-- appsettings.Development.json
|       |
|       |-- Controllers/
|       |-- Data/
|       |-- Entities/
|       |-- Dtos/
|       |-- Services/
|       |-- Enums/
|       |-- Exceptions/
|       |-- Configuration/
|       `-- Auth/                # later
|
`-- tests/
    `-- BPInventory.Api.Tests/   # later
```

The exact physical layout may differ slightly from the existing Visual Studio-created solution. The important decision is the responsibility of each area.

---

## 2. Controllers/

Contains API controllers.

Examples later:

- ProductsController
- CategoriesController
- VendorsController
- RestocksController
- InventoryAdjustmentsController
- AuditLogsController
- AuthController if needed

Controllers should remain thin.

---

## 3. Data/

Contains EF Core persistence infrastructure.

Expected files later:

```text
Data/
|-- ApplicationDbContext.cs
`-- Configurations/
    |-- ProductConfiguration.cs
    |-- CategoryConfiguration.cs
    |-- VendorConfiguration.cs
    |-- RestockEventConfiguration.cs
    |-- RestockItemConfiguration.cs
    |-- InventoryAdjustmentConfiguration.cs
    `-- AuditLogConfiguration.cs
```

Entity configuration classes are recommended once mappings become non-trivial.

This prevents a very large `OnModelCreating` method.

---

## 4. Entities/

Contains EF Core persistence/domain entities.

Expected entities:

- Product
- Category
- Vendor
- RestockEvent
- RestockItem
- InventoryAdjustment
- AuditLog
- ApplicationUser later

These are persistence models, not request DTOs.

---

## 5. Dtos/

Contains API request and response contracts.

A feature-based DTO subfolder structure is preferred once the number of DTOs grows.

Example:

```text
Dtos/
|-- Products/
|   |-- CreateProductRequest.cs
|   |-- UpdateProductRequest.cs
|   `-- ProductResponse.cs
|
|-- Restocks/
|   |-- CreateRestockRequest.cs
|   |-- RestockItemRequest.cs
|   `-- RestockResponse.cs
|
`-- InventoryAdjustments/
    |-- CreateInventoryAdjustmentRequest.cs
    `-- InventoryAdjustmentResponse.cs
```

This prevents one large flat DTO folder.

---

## 6. Services/

Contains service interfaces and implementations.

A simple structure is sufficient:

```text
Services/
|-- IProductService.cs
|-- ProductService.cs
|-- ICategoryService.cs
|-- CategoryService.cs
|-- IVendorService.cs
|-- VendorService.cs
|-- IRestockService.cs
|-- RestockService.cs
|-- IInventoryAdjustmentService.cs
|-- InventoryAdjustmentService.cs
|-- IAuditService.cs
`-- AuditService.cs
```

If this folder becomes too large, it can later be grouped by feature without changing the architecture.

---

## 7. Enums/

Contains controlled domain values that are naturally represented as enums.

Initial example:

- InventoryAdjustmentReason

Avoid creating enums for values that need arbitrary database administration or frequent runtime extension.

---

## 8. Exceptions/

Contains application-specific exception types if the implementation uses exceptions for expected service failures.

Potential examples:

- NotFoundException
- ConflictException
- BusinessRuleException

A central exception handler maps these failures to HTTP ProblemDetails responses.

Do not create dozens of hyper-specific exception classes without need.

---

## 9. Configuration/

Contains strongly typed options classes and configuration-related helpers when needed.

Examples later may include:

- JwtOptions
- other grouped settings

Do not put business configuration tables here; this folder refers to application configuration.

---

## 10. Auth/

Deferred until the authentication phase.

May contain:

- ApplicationUser
- role constants
- policy names
- current-user abstraction
- authentication-related services

Avoid prematurely mixing authentication code throughout every folder.

---

## 11. tests/

A separate test project will be added later.

Likely:

```text
tests/
`-- BPInventory.Api.Tests/
```

Initial focus:

- service/business-rule tests
- later integration tests for API/database behavior

A separate project prevents production code and test-only dependencies from being mixed together.

---

## 12. Why One Main Project?

An alternative architecture might use multiple class libraries:

```text
BPInventory.Api
BPInventory.Application
BPInventory.Domain
BPInventory.Infrastructure
```

This resembles Clean Architecture.

It can be useful in large systems, but it introduces:

- more projects
- more references
- more abstractions
- more navigation overhead
- additional ceremony

For this MVP, logical separation inside one project is enough.

If the project genuinely grows, the boundaries are clear enough to extract into separate projects later.

The architecture should solve current problems rather than optimize for hypothetical scale.
