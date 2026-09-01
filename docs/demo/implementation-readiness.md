# Implementation Readiness Checklist

## Status

The non-coding architecture is considered ready for implementation once this checklist is satisfied.

This document reconciles previous planning documents. Where an earlier note conflicts with this file, the decision below is authoritative until the master implementation specification replaces it.

## Locked Domain Scope

Included:

- single location
- convenience-store merchandise
- Product
- Category
- Vendor
- RestockEvent / RestockItem
- InventoryAdjustment
- low-stock reporting
- AuditLog
- users/roles
- synthetic demo data
- Azure deployment

Excluded:

- POS
- payments
- fuel systems
- barcode hardware
- payroll/scheduling
- purchase orders/invoicing
- multi-store
- forecasting
- mobile app
- microservices

## Locked Application Architecture

```text
Controllers
→ Services
→ ApplicationDbContext
→ EF Core
→ SQL Server
```

- modular monolith
- one main API project initially
- thin controllers
- scoped services
- scoped DbContext
- DTOs separate from entities
- no generic repository
- no MediatR
- no CQRS framework
- no AutoMapper initially

## Locked Inventory Model

`Product.QuantityOnHand` stores current state.

Restock and Adjustment records preserve the explanation/history.

Every inventory mutation must keep current quantity + history consistent atomically.

Product CRUD must never directly change QuantityOnHand.

## Locked Delete/History Model

Soft deactivate:

- Product
- Category
- Vendor
- ApplicationUser

Append-oriented / no ordinary update-delete API:

- RestockEvent
- RestockItem
- InventoryAdjustment
- AuditLog

## Locked API Conventions

Base:

`/api`

Resource routes use plural lowercase nouns.

Master-data updates use PUT.

Master-data DELETE performs soft deactivation.

Explicit `/reactivate` operation restores active state.

Growing collections use page/pageSize pagination.

Status convention:

- 200 read/update
- 201 create
- 204 deactivate/logout where appropriate
- 400 invalid request
- 401 unauthenticated
- 403 forbidden
- 404 not found
- 409 business/state conflict
- 500 unexpected failure

Errors use ProblemDetails/ValidationProblemDetails.

## Locked Health Endpoints

Authoritative:

- `/health` = process/liveness
- `/health/ready` = readiness including database

The earlier design that mentioned only `/health` is superseded.

Azure App Service Health Check targets `/health/ready`.

## Locked Authentication

MVP:

- ASP.NET Core Identity
- secure Identity cookie authentication
- antiforgery on unsafe cookie-authenticated requests
- no public registration

Do NOT implement a custom email/password JWT issuer.

Future enterprise evolution may use Microsoft Entra ID + OIDC/OAuth bearer access tokens.

## Locked Roles

Exactly one business role per user:

- Employee
- Manager
- Admin

Policies:

- EmployeeOrAbove
- ManagerOrAbove
- AdminOnly

## Locked Testing

- xUnit
- dedicated SQL Server test database
- real EF migrations
- WebApplicationFactory for API integration
- no EF InMemory as primary persistence proof
- no repository added just for mocking

Priority:

- restock atomicity
- negative-stock prevention
- auth/RBAC
- historical integrity
- SQL constraints
- API contract

## Locked Logging/Observability

- `ILogger<T>`
- structured logging
- centralized unexpected-exception logging
- AuditLog separate from technical logs
- Azure Monitor OpenTelemetry/Application Insights in Azure
- no Serilog requirement

## Locked Environment Model

- Development
- Testing
- Production

Azure portfolio deployment uses:

`ASPNETCORE_ENVIRONMENT=Production`

Synthetic demo seed uses:

`SeedData:Enabled=true`

Do not use a custom `Demo` hosting environment.

## Locked Azure Runtime

- Azure App Service
- Azure SQL Database
- one demo resource group
- HTTPS
- system-assigned App Service managed identity
- managed identity receives runtime data permissions
- Azure SQL selected-network firewall
- no broad "Allow Azure services" rule
- Private Endpoint/VNet deferred
- Application Insights/OpenTelemetry

## Locked Migration Model

Local:

EF migration tooling.

Testing:

real migrations applied to test DB.

Azure:

migration bundle/deployment step.

Do NOT unconditionally call `Database.Migrate()` on normal Production startup.

Runtime App Service identity and schema-deployment identity remain separate.

## Locked Seed Rules

System:

- Employee
- Manager
- Admin roles

Demo:

- synthetic accounts
- categories/vendors/products
- historical restocks/adjustments

Seeding is idempotent.

No passwords in source.

No real franchise-sensitive data in public deployment.

## CI/CD Scope

GitHub Actions is optional after core application + manual Azure deployment work.

If implemented:

- CI restore/build/test
- SQL Server integration dependency
- Azure deployment via OIDC federation
- no long-lived Azure client password
- migrations remain explicit deployment step

CI/CD must not delay finishing the backend.

## Implementation Order

Codex should implement in controlled vertical milestones rather than generating the entire project blindly:

1. project structure + packages + startup infrastructure
2. entities/enums
3. DbContext + EF configurations
4. initial migration
5. Product/Category/Vendor DTOs/services/controllers
6. restock workflow
7. inventory-adjustment workflow
8. low-stock query
9. AuditLog infrastructure
10. Identity + current-user context
11. authorization policies
12. auth/user administration endpoints
13. global ProblemDetails/error handling
14. seed data
15. health checks/logging
16. automated tests
17. Azure-specific configuration
18. deployment
19. CI/CD only if time
20. final README/demo packaging

Each milestone must compile and be testable before advancing.

## Codex Constraints

Codex must NOT independently introduce:

- repository/unit-of-work wrappers
- Clean Architecture multi-project split
- MediatR/CQRS
- AutoMapper
- JWT token service
- microservices
- Docker production deployment
- Redis
- message queues
- Terraform
- Kubernetes
- frontend frameworks

unless the specification is explicitly revised.

## Ready-to-Implement Definition

Implementation can begin when:

- documents are committed
- Visual Studio solution runs
- local SQL Server is reachable
- Git repository is clean
- master Codex implementation specification has been generated

The master Codex specification becomes the single implementation authority.
