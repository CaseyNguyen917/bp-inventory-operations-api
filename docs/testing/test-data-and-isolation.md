# Test Data and Database Isolation

## Dedicated database

Use:

`BPInventory_Test`

Do not use development, demo, or production data.

## Configuration

Use:

`ConnectionStrings:TestConnection`

Credentials stay outside Git.

## Safety

Before destructive cleanup, verify the configured database is an explicitly approved test database. Abort on mismatch.

## Schema

Apply real EF Core migrations at suite initialization.

## Reset

Keep the schema but reset data between tests/test groups.

Because of foreign keys, child records must be cleared before parents if manual cleanup is used.

## Parallelization

Database-mutating integration tests run serially by default.

Only enable parallel execution after isolation has been deliberately engineered.

## Test builders

Use small helpers such as:

```text
CreateCategoryAsync
CreateVendorAsync
CreateProductAsync
CreateUserAsync
```

Defaults can be valid but every important value remains overridable and visible.

## Determinism

Tests must not depend on:

- current production data
- prior test order
- random collisions
- mutable demo seed data

Use predictable test identities and unique SKUs where necessary.

## Time

Use UTC.

Avoid asserting exact milliseconds unless time is abstracted. Assert meaningful ranges/state instead.

## Authentication users

Create isolated Employee, Manager, and Admin test users.

Do not reuse actual demo or production credentials.
