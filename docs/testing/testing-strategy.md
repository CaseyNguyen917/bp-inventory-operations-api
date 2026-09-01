# Testing Strategy

## Purpose

This project uses automated testing to protect business rules, SQL Server behavior, API contracts, security boundaries, and historical integrity. The goal is not 100% coverage; it is confidence in the parts of the system that can corrupt inventory, expose permissions incorrectly, or regress after changes.

## Framework and test project

Use xUnit in a separate project:

```text
tests/
└── BPInventory.Api.Tests/
```

Recommended structure:

```text
BPInventory.Api.Tests/
├── Unit/
├── Services/
├── Integration/
│   ├── Api/
│   └── Authentication/
├── Infrastructure/
│   ├── TestDatabase.cs
│   ├── CustomWebApplicationFactory.cs
│   ├── TestCurrentUserContext.cs
│   └── TestDataBuilder.cs
└── Fixtures/
```

Use `Microsoft.AspNetCore.Mvc.Testing` for full ASP.NET Core integration tests.

## Test levels

### Pure unit tests

Use only for logic that is genuinely independent of ASP.NET Core and SQL Server, such as deterministic calculations or small policy/helper logic.

Do not restructure the production application just to manufacture unit tests.

### Service + SQL Server integration tests

This is the primary business-rule test layer.

Tests use:

- real service implementations
- real `ApplicationDbContext`
- the real SQL Server EF Core provider
- a dedicated SQL Server test database

Examples:

- duplicate SKU is rejected
- Restock creates header/lines and updates all Product quantities
- a failed Restock does not partially change stock
- a negative-result InventoryAdjustment is rejected
- low-stock LINQ behaves correctly against SQL Server

These are integration tests even when they call a service directly, because EF Core and SQL Server participate.

### Full API integration tests

Use `WebApplicationFactory<Program>` and `HttpClient`.

This exercises:

```text
HTTP
→ middleware
→ authentication/authorization
→ model binding/validation
→ controller
→ service
→ EF Core
→ SQL Server
→ serialized HTTP response
```

High-value examples:

- anonymous protected request returns 401
- Employee POST Product returns 403
- Manager POST Product returns 201
- invalid DTO returns ValidationProblemDetails
- duplicate SKU returns 409 ProblemDetails
- paginated GET returns the documented DTO contract

## Database testing decision

Use a dedicated SQL Server database such as:

`BPInventory_Test`

Never run resettable automated tests against the development, demo, or production database.

Connection string:

`ConnectionStrings:TestConnection`

Store secrets outside Git.

## Why not EF Core InMemory?

Do not use the EF Core InMemory provider as the primary persistence test strategy.

It does not reproduce important relational behavior such as:

- SQL Server query translation
- relational constraints
- provider-specific semantics
- transaction behavior

## Why not mocked DbSet queries?

Mocking or backing `DbSet` with in-memory collections tests LINQ-to-Objects behavior, not SQL Server execution.

Do not use mocked query behavior as evidence that an EF Core query works correctly in production.

## Why not add a Repository only for testing?

The application already decided:

`Service → ApplicationDbContext → SQL Server`

Do not redesign production architecture solely to make mocking easier.

## Test database lifecycle

Recommended flow:

1. Verify configured database is explicitly a test database.
2. Apply real EF Core migrations.
3. Reset test data between tests or isolated test groups.
4. Seed only the minimum records needed.
5. Run shared-database integration tests serially until deliberate parallel isolation exists.

Do not depend on test execution order.

## Safety guard

Any destructive test reset helper must validate the target database name first.

For example, require the configured database name to contain `Test` or exactly match a configured safe name.

If the safety check fails, abort.

## Migrations

Use the real EF Core migrations to construct the integration-test schema.

Do not use `EnsureCreated` as the main substitute for migration coverage.

This helps validate:

- constraints
- indexes
- relationships
- migration compatibility
- production-like schema behavior

## Test data

Prefer small explicit fixtures.

Example:

```text
Category: Beverages
Vendor: Test Beverage Vendor
Product:
  SKU: TEST-COKE
  QuantityOnHand: 10
```

Use helper/builders only to remove repetitive setup without hiding the values that matter to a test.

## Current-user testing

Service tests use a deterministic implementation of `ICurrentUserContext`, not raw `HttpContext`.

Example:

```text
UserId = test-employee-1
Role = Employee
```

## Real authentication tests

Selected API integration tests must exercise the actual Identity cookie flow:

1. seed known test user
2. obtain antiforgery token
3. login through real endpoint
4. retain cookie in test HttpClient
5. call protected endpoint
6. verify role behavior

A controlled test-auth scheme can be used for some non-security tests if needed, but real authentication integration tests remain mandatory.

## Transaction/atomicity tests

### Restock rollback

Initial:

```text
Coke = 10
Sprite = 20
```

Request includes one valid line and one invalid Product.

Expected:

- no RestockEvent
- no RestockItems
- no Product quantity changes
- no success audit event

### Adjustment rollback

Initial quantity = 5.

Request `QuantityChange = -8`.

Expected:

- Product remains 5
- no InventoryAdjustment record
- no success audit event

## Database constraint tests

Verify defense in depth for important constraints:

- duplicate SKU
- invalid FK
- negative Product quantity
- non-positive RestockItem quantity where DB CHECK constraints exist

The service should normally prevent these first, but SQL Server remains the final integrity boundary.

## What not to test

Do not write tests merely proving framework internals, trivial DTO getters, C# arithmetic, or Identity's cryptography itself.

Test our behavior and configuration.

## Coverage priority

Prioritize:

1. inventory-changing workflows
2. atomicity
3. negative-stock prevention
4. authorization/security
5. historical integrity
6. uniqueness/constraints
7. API error semantics
8. query/filter/pagination behavior
9. audit accountability

## Naming

Use behavior-oriented names:

```text
RecordRestock_WithMultipleValidItems_IncreasesAllProductQuantities
RecordRestock_WhenAnyProductIsInvalid_DoesNotPersistPartialChanges
CreateProduct_WithDuplicateSku_ReturnsConflict
RecordAdjustment_WhenResultWouldBeNegative_IsRejected
Employee_WhenCreatingProduct_IsForbidden
```

Use Arrange / Act / Assert.

## Interview summary

> I avoided relying on EF Core's InMemory provider for core persistence tests because it doesn't reproduce relational SQL Server behavior or transaction semantics. My main business-rule tests run services against a dedicated SQL Server test database using the same provider and migrations as production, while API integration tests use WebApplicationFactory to exercise routing, validation, auth, controllers, services, EF Core, and serialization together. I use pure unit tests only where logic is actually infrastructure-independent, and I prioritize inventory atomicity, authorization, historical integrity, and error contracts over an arbitrary coverage percentage.
