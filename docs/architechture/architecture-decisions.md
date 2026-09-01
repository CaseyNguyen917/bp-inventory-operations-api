# Architecture Decisions

This file records major architectural decisions so future implementation does not silently redesign the project.

## ADR-001: Modular Monolith

**Decision:** Build one ASP.NET Core Web API application backed by one SQL Server database.

**Why:** Current business scope does not justify distributed-system complexity.

**Rejected for MVP:** Microservices.

---

## ADR-002: Thin Controllers + Service Layer

**Decision:** Controllers handle HTTP concerns; services own business rules and workflow orchestration.

**Why:** Improves separation of concerns, testability, and reuse of business logic.

---

## ADR-003: EF Core DbContext Used Directly by Services

**Decision:** Do not create a generic repository or custom UnitOfWork wrapper for the MVP.

**Why:** EF Core DbContext/DbSet already provide the required persistence abstraction and unit-of-work behavior. Wrapping every method would add ceremony without solving a current problem.

**Revisit if:** persistence complexity or architectural isolation creates a concrete need.

---

## ADR-004: DTOs Separate from EF Entities

**Decision:** API requests/responses use explicit DTOs.

**Why:** Prevent over-posting, avoid exposing persistence internals, keep API contract independent from EF mappings, and avoid navigation-property serialization issues.

---

## ADR-005: DataAnnotations for Initial Request Validation

**Decision:** Use built-in ASP.NET Core validation/DataAnnotations for straightforward request-shape validation.

**Why:** Requirements are simple enough that an extra validation framework is unnecessary initially.

**Revisit if:** complex conditional validation becomes common.

---

## ADR-006: Centralized ProblemDetails Error Handling

**Decision:** Use centralized ASP.NET Core exception handling and ProblemDetails rather than repetitive controller try/catch logic.

**Why:** Consistent error contracts and simpler controllers.

---

## ADR-007: Scoped Business Services

**Decision:** Business services and ApplicationDbContext use scoped lifetime.

**Why:** Business operations naturally align with HTTP request/unit-of-work boundaries.

---

## ADR-008: Built-in OpenAPI Generation

**Decision:** Generate OpenAPI documentation from ASP.NET Core.

**Why:** Provides machine-readable API documentation and supports interactive development/demo tooling.

---

## ADR-009: Configuration Through ASP.NET Core Configuration System

**Decision:** Environment-dependent values are supplied by appsettings, development secrets, environment variables, or Azure settings rather than hard-coded.

**Why:** Enables environment separation and avoids committing secrets.

---

## ADR-010: No AutoMapper Initially

**Decision:** DTO mapping will initially be explicit/manual.

**Why:** Mappings are small enough that explicit code is easier to understand and debug.

**Revisit if:** mapping volume creates meaningful repetitive boilerplate.

---

## ADR-011: One Main API Project Initially

**Decision:** Keep logical layers as folders/modules inside one ASP.NET Core project.

**Why:** Separate Domain/Application/Infrastructure projects would add ceremony without a current deployment or team-boundary requirement.

**Revisit if:** project size or architecture demands stronger compile-time boundaries.
