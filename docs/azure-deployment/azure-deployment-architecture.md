# Azure Deployment Architecture

## 1. Purpose

This document defines the Azure architecture for the portfolio/demo deployment.

The deployment intentionally demonstrates important Azure backend concepts without adding Kubernetes, containers, Terraform, private networking, or other infrastructure that does not solve a current requirement.

---

## 2. High-Level Architecture

```text
Internet / Browser / API Client
          |
        HTTPS
          |
          v
Azure App Service
ASP.NET Core Web API
          |
          | passwordless Microsoft Entra authentication
          | via system-assigned managed identity
          v
Azure SQL Database
          |
          +-- Identity tables
          +-- Product/Category/Vendor
          +-- Restock history
          +-- Inventory adjustments
          +-- AuditLog

Azure App Service
          |
          +--> Azure Monitor / Application Insights
          |
          +--> Health endpoints
```

---

## 3. Resource Group

Use one Azure resource group for the portfolio deployment because the resources share the same lifecycle.

Recommended name:

`rg-bpinventory-demo`

The group contains the app, database, monitoring resources, and related infrastructure.

This makes cleanup straightforward:

Deleting the resource group removes the related portfolio deployment resources together.

---

## 4. Region

Place the application and database in the same Azure region where practical.

Benefits:

- lower application-to-database latency
- simpler architecture
- easier cost/operations reasoning

Example candidate:

`East US`

The final deployment region can change based on service availability, quota, and current pricing.

---

## 5. Azure App Service

Use Azure App Service to host the ASP.NET Core API.

Target:

- Linux App Service
- supported .NET 10 runtime
- HTTPS-only public application
- system-assigned managed identity enabled

Why App Service?

- managed PaaS hosting
- no VM administration
- integrated .NET support
- managed identity
- environment configuration
- health checks
- deployment integrations
- Azure Monitor integration

The project does not need to manage:

- operating-system patching
- IIS/NGINX server administration
- VM scaling infrastructure

---

## 6. App Service Plan

Do not hard-code a pricing SKU into the architecture documentation because availability, student credits, and pricing can change.

Deployment policy:

1. start with the lowest-cost App Service tier that supports the required portfolio features
2. use the Azure pricing calculator before creating resources
3. target a small Basic-class plan if a free tier is too limited for a reliable public demo
4. scale down or delete the deployment when it is not needed

The backend is intentionally designed so no code change is required when the App Service plan changes.

---

## 7. Azure SQL Database

Use:

Azure SQL Database

not:

- SQL Server running in a VM
- Cosmos DB
- PostgreSQL
- a self-managed database container

Azure SQL preserves the SQL Server/EF Core technology story while moving database operations to a managed PaaS service.

---

## 8. Azure SQL Compute Choice

The demo workload is expected to be:

- small
- intermittent
- unpredictable
- idle for long periods

A strong deployment candidate is Azure SQL Database General Purpose serverless with auto-pause.

Deployment target:

- smallest practical serverless compute settings
- target minimum around 0.5 vCore if available
- small maximum compute limit
- approximately 60-minute auto-pause delay

This must be verified against current Azure pricing at deployment time.

Why serverless?

During true inactivity, compute can pause and only storage remains billed.

Tradeoff:

The first request after a paused period can experience a cold-start/resume delay.

This is acceptable for a portfolio demo but would need reconsideration for a latency-sensitive store production system.

---

## 9. Azure SQL Logical Server

Azure SQL Database lives under an Azure SQL logical server.

Conceptually:

```text
Azure SQL logical server
    |
    `-- BPInventory database
```

The logical server provides shared server-level configuration such as networking and Microsoft Entra administration.

It is not a VM and does not mean we manage a SQL Server operating system.

---

## 10. Monitoring

Create Application Insights / Azure Monitor monitoring for the API.

The application uses:

`Azure.Monitor.OpenTelemetry.AspNetCore`

Production telemetry connection information is supplied through:

`APPLICATIONINSIGHTS_CONNECTION_STRING`

The monitoring design covers:

- HTTP requests
- exceptions
- dependencies such as SQL
- traces
- logs
- metrics

Business AuditLog remains in SQL Server and is not replaced by Application Insights.

---

## 11. Health Monitoring

Application endpoints:

- `/health` — liveness
- `/health/ready` — readiness including database connectivity

Configure Azure App Service Health Check to use:

`/health/ready`

The health endpoint exposes status only and never returns connection strings or secrets.

---

## 12. Public API Access

The portfolio API remains publicly reachable over HTTPS so it can be demonstrated.

Application-level controls provide:

- authentication
- role-based authorization
- antiforgery protection
- secure cookies

This does not mean the database is publicly open to every client.

Only the App Service and explicitly authorized administrative/development network paths should reach Azure SQL.

---

## 13. Why Not Private Endpoint for MVP?

A production enterprise architecture could use:

```text
App Service
  -> VNet Integration
  -> Azure SQL Private Endpoint
```

and disable SQL public network access entirely.

That improves network isolation but introduces:

- virtual networks
- subnets
- private DNS
- private-endpoint cost
- more App Service plan/network configuration
- more troubleshooting complexity

For this portfolio MVP, public SQL networking with narrow firewall allow-lists plus managed identity is a deliberate complexity tradeoff.

Private networking is documented as a future hardening step.

---

## 14. No Containers Required

ASP.NET Core runs directly on the managed App Service runtime.

Docker is not required.

This keeps focus on:

- ASP.NET Core
- SQL
- EF Core
- authentication
- Azure PaaS
- cloud configuration

rather than container operations.

---

## 15. Interview Summary

> I deployed the backend as a modular ASP.NET Core application on Azure App Service with Azure SQL Database. I chose PaaS services so I could focus on application architecture instead of VM and operating-system management. The App Service uses a system-assigned managed identity for passwordless Azure SQL authentication, monitoring flows through Azure Monitor/Application Insights using OpenTelemetry, and separate liveness/readiness endpoints support operational checks. For the portfolio workload I favor a small cost-controlled App Service plan and an intermittent Azure SQL serverless configuration, while documenting private networking as a production hardening path rather than adding VNet complexity to the MVP.
