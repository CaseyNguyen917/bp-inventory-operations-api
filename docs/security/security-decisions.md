# Security Architecture Decisions

## ADR-SEC-001: ASP.NET Core Identity

**Decision:** Use ASP.NET Core Identity for users, passwords, roles, lockout, and sign-in infrastructure.

**Why:** Mature framework integration avoids building security-sensitive identity primitives manually.

---

## ADR-SEC-002: Cookie Authentication for MVP

**Decision:** Use Identity application cookies rather than custom JWT issuance.

**Why:** The expected system is an internal browser-oriented application, and secure HttpOnly cookies integrate naturally with Identity.

**Future:** Standards-based OIDC/OAuth bearer authentication can replace this when external/mobile/enterprise clients justify it.

---

## ADR-SEC-003: No Custom Username/Password JWT Issuer

**Decision:** Do not build a TokenService that mints custom production JWTs directly after receiving email/password.

**Why:** Secure access-token issuance is an identity-provider responsibility and introduces unnecessary security complexity.

---

## ADR-SEC-004: No Public Registration

**Decision:** Accounts are provisioned by Admins.

**Why:** This is an internal workforce application, not a consumer application.

---

## ADR-SEC-005: Exactly One Business Role

**Decision:** Every user has exactly one of Employee, Manager, Admin.

**Why:** Simplifies the business permission model while Identity still provides normal role infrastructure.

---

## ADR-SEC-006: Policies Express Hierarchy

**Decision:** Define EmployeeOrAbove, ManagerOrAbove, and AdminOnly policies.

**Why:** Identity roles do not inherently have hierarchy. Policies centralize the intended hierarchy.

---

## ADR-SEC-007: Current User Comes from Server Identity

**Decision:** Services use ICurrentUserContext.

**Why:** Clients must not control RecordedByUserId or audit actor identity.

---

## ADR-SEC-008: Cookie-Based Requests Require CSRF Protection

**Decision:** Unsafe HTTP methods require an antiforgery request token.

**Why:** Browsers automatically attach authentication cookies, which creates CSRF risk.

---

## ADR-SEC-009: Same-Origin First

**Decision:** Do not enable broad CORS. Prefer hosting any simple frontend from the same origin.

**Why:** Simplifies cookie security and reduces unnecessary cross-origin attack/configuration surface.

---

## ADR-SEC-010: Soft-Deactivate Users

**Decision:** Do not physically delete users referenced by history.

**Why:** Restocks, adjustments, and audit entries must remain attributable.

---

## ADR-SEC-011: Protect the Final Admin

**Decision:** Reject operations that would leave no active Admin.

**Why:** Prevent accidental administrative lockout.

---

## ADR-SEC-012: User Secrets Locally, Azure Configuration in Deployment

**Decision:** Secrets stay outside Git.

**Why:** Source control is not a secret store.

---

## ADR-SEC-013: No 2FA / Email Recovery in MVP

**Decision:** Defer these workflows.

**Why:** They require additional infrastructure and are not necessary to demonstrate the project's core backend/authentication architecture.

**Future:** Strong candidates for production hardening.
