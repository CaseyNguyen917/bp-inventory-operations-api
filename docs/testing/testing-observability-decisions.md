# Testing and Observability Decisions

## ADR-TEST-001 — xUnit
Use xUnit with `dotnet test`.

## ADR-TEST-002 — Real SQL Server
Core persistence/business integration tests use a dedicated SQL Server test database.

## ADR-TEST-003 — No EF InMemory as Primary Test DB
It does not reproduce relational SQL Server semantics sufficiently.

## ADR-TEST-004 — No Repository Added Just for Mocks
Preserve Service → DbContext architecture.

## ADR-TEST-005 — WebApplicationFactory
Use `WebApplicationFactory<Program>` for full HTTP integration tests.

## ADR-TEST-006 — Migrations Build Test Schema
Use real migrations instead of relying on `EnsureCreated`.

## ADR-TEST-007 — Risk-Based Coverage
Prioritize inventory atomicity, permissions, historical integrity, constraints, and API contracts rather than an arbitrary 100% target.

## ADR-OBS-001 — ILogger<T>
Application code uses built-in structured logging abstraction.

## ADR-OBS-002 — Audit ≠ Technical Logging
SQL AuditLog and operational telemetry remain separate.

## ADR-OBS-003 — Liveness + Readiness
`/health` is liveness; `/health/ready` includes database readiness.

## ADR-OBS-004 — Azure Monitor OpenTelemetry
Use Azure Monitor OpenTelemetry/Application Insights after Azure deployment.

## ADR-OBS-005 — No Serilog Requirement
Do not add Serilog unless a concrete sink/enrichment need appears.

## ADR-OBS-006 — Never Log Secrets
Passwords, cookies/tokens, antiforgery tokens, credentials, and sensitive configuration are excluded from logs.
