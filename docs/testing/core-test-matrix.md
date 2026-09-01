# Core Test Matrix

This is the minimum strong automated test set.

## Product

### Create
- valid Manager create succeeds
- QuantityOnHand starts at zero
- Product starts active
- duplicate SKU rejected
- missing Category rejected
- missing Vendor rejected
- inactive Category/Vendor rejected for normal creation
- negative cost rejected
- negative retail price rejected
- negative reorder threshold rejected

### Read/List
- existing Product returned
- nonexistent Product returns 404
- inactive excluded by default
- includeInactive includes inactive
- search by Name
- search by SKU
- Category filter
- Vendor filter
- sorting
- pagination metadata

### Update
- valid metadata update succeeds
- quantity cannot be edited via Product DTO
- duplicate SKU rejected
- missing Product rejected

### Deactivate/Reactivate
- soft delete sets IsActive false
- row remains persisted
- historical references remain valid
- reactivation succeeds when valid

### Low Stock
- below threshold included
- equal threshold included
- above threshold excluded
- inactive excluded
- category/vendor filters work

## Category
- create
- duplicate name rejected
- update
- soft deactivate
- reactivate
- relationship with Product preserved

## Vendor
- create
- duplicate name rejected
- update contact data
- soft deactivate
- reactivate
- historical Restock references preserved

## Restock

### Valid
- one line increases inventory
- multiple lines update all Products
- RestockEvent persisted
- RestockItems persisted
- Vendor stored
- authenticated user stored
- audit entry created

### Invalid
- empty items rejected
- zero quantity rejected
- negative quantity rejected
- missing Vendor rejected
- missing Product rejected
- duplicate Product lines rejected
- inactive Vendor/Product rejected

### Atomicity
- one invalid line prevents all changes
- no partial Product update
- no RestockEvent
- no RestockItems
- no success audit entry

## Inventory Adjustment

### Valid
- positive adjustment increases stock
- negative adjustment decreases stock
- reason/notes persisted
- authenticated user persisted
- audit entry created

### Invalid
- zero change rejected
- missing Product rejected
- invalid reason rejected
- resulting negative stock rejected
- inactive Product rejected

### Atomicity
- rejected adjustment leaves Product unchanged
- no history record
- no success audit entry

## Audit
- Product create/update/deactivate audited
- Restock audited
- Adjustment audited
- actor comes from server identity
- client cannot spoof actor
- Employee denied audit API
- Manager allowed
- filters/pagination work

## Authentication
- valid active user login
- invalid password rejected
- nonexistent email gives generic failure
- inactive user denied login
- lockout after configured attempts
- logout invalidates session
- `/api/auth/me` returns current identity
- change password validates current password/policy

## Authorization
- anonymous protected request -> 401
- Employee GET Product allowed
- Employee POST Product -> 403
- Manager POST Product allowed
- Manager user administration -> 403
- Admin user administration allowed
- Employee Restock allowed
- Employee Adjustment allowed
- Employee AuditLog -> 403
- Manager AuditLog allowed

## Antiforgery
- unsafe authenticated request without token rejected
- same request with valid token succeeds
- GET does not require token
- login works with token

## User Administration
- Admin creates Employee
- duplicate email rejected
- invalid role rejected
- role change replaces old role
- inactive user cannot log in
- final active Admin cannot be demoted
- final active Admin cannot be deactivated
- self-deactivation rejected
- historical actor references survive deactivation

## Error Contract
- DTO validation -> 400 ValidationProblemDetails
- missing resource -> 404 ProblemDetails
- duplicate SKU -> 409 ProblemDetails
- negative-stock conflict -> 409 ProblemDetails
- unauthenticated -> 401
- forbidden -> 403
- unexpected error -> generic 500 without sensitive internals

## Health
- `/health` returns healthy when process runs
- `/health/ready` reports SQL Server readiness
- health output never exposes secrets
