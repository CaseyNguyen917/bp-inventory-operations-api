# API Design Decisions

## ADR-API-001: Resource-Oriented Routes

**Decision:** Use plural noun-based REST-style routes.

**Examples:** `/api/products`, `/api/vendors`, `/api/restocks`.

**Why:** Clear resource semantics and conventional HTTP design.

---

## ADR-API-002: DTOs Define the Contract

**Decision:** Request and response DTOs are explicit and separate from EF Core entities.

**Why:** Protect server-owned fields and decouple HTTP contract from persistence.

---

## ADR-API-003: Product Creation Starts Inventory at Zero

**Decision:** `CreateProductRequest` cannot set QuantityOnHand.

**Why:** Preserve the rule that inventory changes have explicit transactional explanations.

**Opening stock:** use a positive InventoryAdjustment such as `PhysicalCountCorrection`.

---

## ADR-API-004: PUT for Master-Data Updates

**Decision:** Use PUT for Product, Category, and Vendor editable metadata.

**Why:** Small editable contracts do not justify PATCH complexity in the MVP.

---

## ADR-API-005: DELETE Performs Soft Deactivation

**Decision:** DELETE Product/Category/Vendor maps to `IsActive = false`.

**Why:** HTTP removal semantics do not require physical SQL deletion, and historical relationships must remain intact.

---

## ADR-API-006: Explicit Reactivation Operation

**Decision:** Use `POST /api/{resource}/{id}/reactivate`.

**Why:** Reactivation is a clear domain state transition outside the normal editable metadata contract.

---

## ADR-API-007: Historical Transactions Are Append-Oriented

**Decision:** Restocks, InventoryAdjustments, and AuditLogs have no normal public update/delete endpoints.

**Why:** Arbitrary mutation undermines traceability.

---

## ADR-API-008: Pagination from the Beginning

**Decision:** Growing collection endpoints use `PagedResponse<T>`.

**Defaults:** page 1, pageSize 25, maximum 100.

**Why:** Prevent unbounded collection responses and establish a consistent client contract.

---

## ADR-API-009: Allow-Listed Filtering and Sorting

**Decision:** Support specific query parameters and sort fields rather than arbitrary expressions.

**Why:** Simpler contract, safer implementation, predictable SQL.

---

## ADR-API-010: ProblemDetails-Style Errors

**Decision:** Use consistent ProblemDetails / ValidationProblemDetails responses.

**Why:** Avoid endpoint-specific error shapes and align with ASP.NET Core API infrastructure.

---

## ADR-API-011: 409 for State/Business Conflicts

**Decision:** Use 409 Conflict for uniqueness/state conflicts such as duplicate SKU or inventory going negative.

**Why:** Distinguishes valid-shaped requests that cannot be applied to current state from static input validation failures.

---

## ADR-API-012: Acting User Comes from Authentication

**Decision:** Operational requests do not accept RecordedByUserId from clients.

**Why:** A client must not impersonate another actor by changing a request field.

---

## ADR-API-013: UTC API Timestamps

**Decision:** Input/output timestamps use ISO-8601 UTC.

**Why:** Consistent server-side temporal semantics and future Azure deployment portability.

---

## ADR-API-014: No API Version Prefix Initially

**Decision:** Use `/api/products`, not `/api/v1/products`.

**Why:** No external backward-compatibility requirement exists yet.

**Revisit when:** a breaking contract change must coexist with an older client contract.
