# README and Demo Strategy

## Purpose

The repository README should communicate the project in under two minutes to a recruiter or engineer, while deeper design reasoning remains in `/docs`.

Do not make the README a textbook.

## Final README Structure

### 1. Title

`BP Franchise Inventory & Operations Management System`

### 2. One-Paragraph Summary

Explain that this is a single-location back-office convenience-store inventory/operations backend inspired by a real franchise workflow and demonstrated with synthetic data.

### 3. Why This Project Exists

Brief business problem:

- maintain merchandise catalog
- track current inventory
- record deliveries/restocks
- record accountable adjustments
- identify low stock
- preserve history
- control access by role

### 4. Tech Stack

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server / Azure SQL Database
- ASP.NET Core Identity
- xUnit
- Azure App Service
- Azure Monitor / Application Insights
- GitHub Actions if implemented

Do not list technologies that are merely planned and unfinished.

### 5. Architecture Diagram

Show:

```text
Client
→ ASP.NET Core Controllers
→ Services
→ EF Core / ApplicationDbContext
→ SQL Server / Azure SQL
```

Include cross-cutting:

- Identity/RBAC
- ProblemDetails
- logging/monitoring
- AuditLog

### 6. Core Features

Only implemented features:

- Product CRUD + soft deletion
- Category/Vendor management
- Restocks
- Inventory Adjustments
- low-stock report
- audit history
- role-based access
- seed/demo data
- Azure deployment

### 7. Key Engineering Decisions

Brief bullets linking to `/docs`:

- modular monolith
- DTO/entity separation
- no generic repository
- transactional inventory mutations
- soft deletion
- append-oriented history
- managed identity in Azure
- real SQL Server integration tests

### 8. Data Model

Embed/link ER diagram.

### 9. API

Link generated OpenAPI documentation or summarize major endpoints.

### 10. Local Setup

Only after implementation is known.

Include:

- prerequisites
- configuration/User Secrets
- database migration
- seed setup
- run command

Never include committed passwords.

### 11. Testing

Explain:

```text
dotnet test
```

and that core persistence tests run against SQL Server semantics.

### 12. Azure Deployment

Brief architecture and, if live, demo/API URL.

### 13. Screenshots / Demo

If there is no meaningful frontend, do not invent one just for screenshots.

Better evidence:

- OpenAPI interface screenshot
- architecture diagram
- ER diagram
- Application Insights screenshot with secrets hidden
- demo GIF/video if useful

### 14. Project Scope

State intentional exclusions:

- POS/payment integration
- fuel-pump integration
- multi-store
- microservices
- heavy frontend

This makes scope look deliberate rather than incomplete.

## Canonical Demo Flow

The final project demo should be approximately 5–8 minutes.

### Step 1 — Architecture

30–60 seconds:

- business problem
- modular-monolith architecture
- SQL Server/Azure SQL

### Step 2 — Employee Login

Demonstrate:

- authentication
- Employee role

### Step 3 — Low Stock

Call:

`GET /api/products/low-stock`

Identify an intentionally low Product.

### Step 4 — Restock

Record a multi-item delivery.

Show:

- `201 Created`
- Product stock increases
- Restock history exists

### Step 5 — Inventory Adjustment

Record damaged/spoiled stock.

Show:

- quantity changes
- history remains

### Step 6 — Authorization

Attempt Manager-only operation as Employee.

Show:

`403 Forbidden`

Then sign in as Manager and perform it successfully.

### Step 7 — Audit

Manager views AuditLog and shows attributable operations.

### Step 8 — Admin

Briefly demonstrate Admin user/role management.

### Step 9 — Azure

Show:

- deployed App Service URL
- `/health/ready`
- Application Insights request/dependency telemetry

### Step 10 — Engineering Decisions

Close with:

- atomic inventory
- DTO separation
- SQL integration tests
- managed identity
- scope choices

## README Honesty Rules

Never claim:

- real production deployment if it is only demo
- real BP corporate integration
- POS integration
- fuel-system integration
- multi-store support
- CI/CD if not implemented
- features that are only designed

Good wording:

> Inspired by the operational needs of a real independently owned BP franchise; the public deployment uses synthetic demonstration data.

## Resume Evidence

The README should give reviewers evidence for resume claims through:

- documented architecture
- database diagram
- test strategy/results
- deployed API
- API docs
- Azure architecture
- commit history
