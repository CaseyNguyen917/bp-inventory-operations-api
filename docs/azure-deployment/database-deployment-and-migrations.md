# Database Deployment, Migrations, and Seed Data

## 1. Migration Philosophy

EF Core migrations are source-controlled schema changes.

Examples:

```text
CreateInitialSchema
AddIdentity
AddAuditIndexes
```

The migration files are part of the application repository.

Never manually redesign Production tables in Azure Portal/SSMS without reflecting the change in EF Core migrations.

---

## 2. Local Development

Normal workflow:

1. change EF entity/configuration
2. create migration
3. review generated migration
4. apply migration to local SQL Server
5. test application
6. commit migration with code

Typical tooling:

```text
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

Visual Studio Package Manager Console equivalents are also acceptable.

---

## 3. Testing

Integration tests apply the real migrations to:

`BPInventory_Test`

This validates the deployable schema path.

---

## 4. Production/Azure Decision

Do NOT call:

`Database.Migrate()`

unconditionally during normal web-application startup.

Why?

The runtime App Service identity should not have schema-change privileges, and multiple application instances should not be responsible for coordinating deployment schema changes.

---

## 5. Migration Bundle

For Azure deployment, generate an EF Core migration bundle.

Conceptually:

```text
dotnet ef migrations bundle
```

A migration bundle packages pending EF Core migrations as a deployment artifact.

The deployment process runs the bundle using a deployment identity with schema-change permission.

This is separate from the normal App Service runtime identity.

---

## 6. Deployment Identity

Schema deployment requires more privilege than runtime CRUD.

Use an explicit administrator/deployment identity with the minimum schema permissions required.

Possible MVP deployment execution:

- authenticated developer/administrator manually runs the migration bundle during first deployment

Future GitHub Actions:

- federated deployment identity authenticates to Azure
- secure migration step applies the bundle
- application deploy occurs after successful migration

The exact database role can be refined when the deployment identity is created.

Do not give the App Service runtime managed identity permanent `db_owner` merely to make migrations easy.

---

## 7. Migration Order

Recommended deployment sequence:

```text
Build/test
    ↓
Create deployment artifact
    ↓
Apply database migration bundle
    ↓
Deploy compatible application
    ↓
Health/readiness check
    ↓
Smoke test
```

For future breaking schema changes, prefer backward-compatible migration patterns where possible.

---

## 8. Migration Review

Before applying a migration to Azure, inspect:

- tables created/dropped
- columns added/removed
- FK behavior
- indexes
- uniqueness constraints
- check constraints
- data-loss operations

Do not blindly approve migrations because EF generated them.

---

## 9. Demo Seed Data

Two seed categories exist.

### Framework/system seed

Safe, required definitions:

- Employee role
- Manager role
- Admin role

These should be created idempotently.

### Demo/business seed

Examples:

- demo Employee/Manager/Admin
- Products
- Categories
- Vendors
- sample inventory/history

Only seed these when:

`SeedData:Enabled = true`

---

## 10. Demo Passwords

Never hard-code demo passwords in source.

Azure receives them through environment configuration.

After users exist, deployment credentials may be removed from Azure settings if the seeding implementation no longer requires them.

The seeder never logs passwords.

---

## 11. Idempotent Seeding

Running the seeder multiple times must not:

- duplicate Products
- duplicate roles
- recreate Users
- overwrite existing user passwords automatically
- create duplicate Vendors/Categories

Use stable identifiers such as:

- role name
- user email
- Product SKU
- Category name
- Vendor name

to detect already-existing seed records.

---

## 12. Production Store vs Portfolio Demo

The portfolio Azure deployment is a demo environment, not the real franchise's production database.

Do not upload:

- real employee credentials
- private operational records
- sensitive supplier data
- actual customer/payment information

Use synthetic demonstration data.

---

## 13. Interview Summary

> I treat EF Core migrations as deployable, source-controlled schema changes rather than letting the application mutate its own schema at startup. Local and test environments use the EF tools directly, while Azure deployment uses a migration bundle executed with a separate deployment identity. The App Service's normal managed identity retains only runtime data permissions. Seed roles are idempotent, and portfolio demo users/business data are enabled through explicit environment configuration with passwords supplied outside source control.
