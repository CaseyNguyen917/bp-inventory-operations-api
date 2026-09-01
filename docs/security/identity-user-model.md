# Identity and User Data Model

## 1. Identity Database Integration

`ApplicationDbContext` should inherit from the appropriate ASP.NET Core Identity EF Core context so domain tables and Identity tables share the same SQL Server database and EF Core migration history.

Conceptually:

```text
ApplicationDbContext
    |
    +-- Identity users/roles tables
    |
    +-- Products
    +-- Categories
    +-- Vendors
    +-- RestockEvents
    +-- RestockItems
    +-- InventoryAdjustments
    +-- AuditLogs
```

One database is appropriate for the modular-monolith MVP.

---

# 2. ApplicationUser

Conceptual C# model:

```text
ApplicationUser : IdentityUser

DisplayName
IsActive
CreatedAtUtc
```

Do not duplicate fields already provided by Identity.

---

# 3. Identity Primary Key

Use Identity's default string user key.

Domain references such as:

- RestockEvent.RecordedByUserId
- InventoryAdjustment.RecordedByUserId
- AuditLog.UserId

use the same Identity user key type.

---

# 4. Roles

Use IdentityRole and Identity's normal user-role relationship table.

Seed role names:

- Employee
- Manager
- Admin

Role names should be represented as application constants to avoid typo-based authorization bugs.

---

# 5. Exactly One Business Role

Although Identity supports many roles per user, application services enforce exactly one of:

- Employee
- Manager
- Admin

This is a business invariant, not an Identity limitation.

Role changes replace the previous business role rather than accumulating several hierarchy roles.

The `ManagerOrAbove` and `EmployeeOrAbove` policies explicitly allow higher roles.

---

# 6. User Foreign-Key Relationships

Conceptual relationships:

```text
ApplicationUser 1 ---- * RestockEvent
ApplicationUser 1 ---- * InventoryAdjustment
ApplicationUser 1 ---- * AuditLog
```

RestockEvent and InventoryAdjustment require an authenticated actor.

AuditLog.UserId may be nullable for system-generated events.

---

# 7. Delete Behavior

Application users should not be physically deleted after they are referenced by historical records.

Use:

`ApplicationUser.IsActive = false`

This mirrors Product/Vendor/Category historical-integrity rules.

Historical transactions must remain attributable to the original user.

---

# 8. Role and Account Security Changes

When:

- role changes
- account is deactivated
- important credentials change

update the user's security stamp where appropriate.

This helps invalidate previously issued authentication sessions during Identity's security-stamp revalidation cycle.

---

# 9. Seed Data

## Roles

Always seed the three role definitions.

Role seed operations should be idempotent.

## Demo users

Development/demo environments may seed:

- Employee demo user
- Manager demo user
- Admin demo user

Do not hard-code their passwords in source control.

Credentials come from development User Secrets or deployment environment configuration.

Example configuration keys:

```text
SeedUsers:AdminEmail
SeedUsers:AdminPassword
SeedUsers:ManagerEmail
SeedUsers:ManagerPassword
SeedUsers:EmployeeEmail
SeedUsers:EmployeePassword
```

Environment-variable equivalent:

```text
SeedUsers__AdminEmail
SeedUsers__AdminPassword
```

The seeder should not log passwords.

Production/demo deployment should avoid repeatedly changing existing user passwords on every startup.

---

# 10. Final Active Admin Rule

The system must preserve at least one active Admin.

Admin user-management services must reject:

- demoting the final active Admin
- deactivating the final active Admin

Also reject self-deactivation through the normal Admin endpoint.

Purpose:

Prevent accidental administrative lockout.

This is an application business/security rule rather than an Identity framework feature.
