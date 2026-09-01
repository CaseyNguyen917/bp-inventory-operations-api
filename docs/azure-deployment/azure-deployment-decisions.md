# Azure Deployment Decisions

## ADR-AZ-001 — Azure App Service

Use Azure App Service for the ASP.NET Core API.

Reason: managed PaaS hosting aligns with the Microsoft/Azure backend target without VM administration.

---

## ADR-AZ-002 — Azure SQL Database

Use Azure SQL Database for deployment.

Reason: preserves SQL Server/EF Core architecture while moving the database to managed Azure PaaS.

---

## ADR-AZ-003 — One Demo Resource Group

Place portfolio resources with the same lifecycle in `rg-bpinventory-demo`.

Reason: organization, cost visibility, and easy cleanup.

---

## ADR-AZ-004 — Managed Identity for Runtime SQL Authentication

App Service uses a system-assigned managed identity.

Reason: no SQL password or long-lived runtime database credential.

---

## ADR-AZ-005 — Separate Runtime and Migration Privileges

Runtime identity receives DML/data permissions.

Schema migrations use a separate deployment/admin identity.

Reason: least privilege.

---

## ADR-AZ-006 — No Runtime Auto-Migration

Do not call unconditional `Database.Migrate()` at application startup.

Production migrations use a reviewed EF Core migration bundle.

Reason: schema change is a deployment concern, not normal request-serving behavior.

---

## ADR-AZ-007 — Selected-Network SQL Firewall

Do not enable broad "Allow Azure services" access.

Allow App Service outbound addresses and temporary authorized admin/developer IPs.

Reason: reduce network exposure without VNet complexity.

---

## ADR-AZ-008 — Private Networking Deferred

VNet Integration + Azure SQL Private Endpoint are future hardening.

Reason: valuable in sensitive production workloads but not required for a portfolio MVP.

---

## ADR-AZ-009 — Production Environment + Explicit Demo Seed Flag

Azure runs `ASPNETCORE_ENVIRONMENT=Production`.

Portfolio seed data is controlled by `SeedData:Enabled`.

Reason: demo data should not disable Production framework/security behavior.

---

## ADR-AZ-010 — Azure Monitor OpenTelemetry

Application Insights telemetry uses Azure Monitor's OpenTelemetry distribution.

Reason: modern Azure observability path and transferable OpenTelemetry knowledge.

---

## ADR-AZ-011 — Cost Is a Requirement

Use the lowest suitable service tiers, budget alerts, and one disposable resource group.

Reason: cloud architecture must include FinOps/cost control.

---

## ADR-AZ-012 — GitHub Actions OIDC If CI/CD Is Added

Future automated Azure deployment authenticates from GitHub using OpenID Connect federation rather than a long-lived Azure password/publish credential where permissions allow.

Reason: short-lived federated cloud credentials are safer and demonstrate modern CI/CD security.
