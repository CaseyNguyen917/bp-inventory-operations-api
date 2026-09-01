# Authentication, Authorization, and Security Architecture

## 1. Purpose

This document defines the authentication, authorization, identity, and application-security design for the BP Franchise Inventory & Operations Management System MVP.

The security model is intentionally appropriate for:

- a single internal business application
- a browser-oriented administrative/operations interface if a frontend is later added
- local development through Visual Studio/OpenAPI tooling
- deployment to Azure App Service
- a small set of employees, managers, and administrators

The design prioritizes secure framework defaults and avoids implementing a custom identity provider or token server.

---

# 2. Authentication Decision

## MVP decision

Use:

- ASP.NET Core Identity
- SQL Server-backed Identity stores through Entity Framework Core
- ASP.NET Core Identity application cookie authentication
- role-based and policy-based authorization

Do NOT create custom username/password JWT access tokens.

## Why cookies?

The expected application is an internal browser-oriented business application.

A secure authentication cookie:

- is handled by the browser automatically
- can be marked HttpOnly so JavaScript cannot read it
- can be marked Secure so it is transmitted only over HTTPS
- can use SameSite restrictions
- integrates directly with ASP.NET Core Identity and SignInManager

The browser client does not need to manually inspect or persist an access token.

---

# 3. Why We Are Not Hand-Rolling JWT Authentication

JWT bearer authentication is important and should be understood, but creating a secure token issuer is a different responsibility from validating tokens in an API.

A production-grade token system requires careful decisions around:

- standardized OAuth/OIDC flows
- signing keys
- key rotation
- issuer and audience validation
- access-token lifetime
- refresh-token lifecycle
- revocation/session invalidation
- secure client storage
- phishing and credential flows

The MVP does not need to become an identity provider.

If the application later requires external/mobile clients, several independent APIs, enterprise single sign-on, or delegated authorization, the preferred evolution is a standards-based identity provider such as Microsoft Entra ID / Entra External ID using OpenID Connect and OAuth 2.0.

The API would then validate bearer access tokens rather than minting its own ad hoc JWTs.

---

# 4. ASP.NET Core Identity Responsibilities

Identity will provide the infrastructure for:

- user storage
- password hashing
- password verification
- role storage
- user-role relationships
- authentication cookies
- lockout behavior
- security stamps
- password policy
- user management APIs used by our services

The application must not:

- store plaintext passwords
- implement its own password hashing algorithm
- compare raw password strings stored in the database
- create a duplicate custom password table

---

# 5. ApplicationUser

Create:

`ApplicationUser : IdentityUser`

Additional domain/application properties:

- `DisplayName`
- `IsActive`
- `CreatedAtUtc`

Identity already supplies properties such as:

- `Id`
- `UserName`
- `NormalizedUserName`
- `Email`
- `NormalizedEmail`
- `EmailConfirmed`
- `PasswordHash`
- `SecurityStamp`
- `ConcurrencyStamp`
- `PhoneNumber`
- lockout-related fields

## Login identifier

The MVP uses email as the human login identifier.

`UserName` should be set consistently from the email internally.

Email must be unique.

---

# 6. Roles

Exactly three business roles exist:

- `Employee`
- `Manager`
- `Admin`

ASP.NET Core Identity technically supports users belonging to multiple roles.

The BP Inventory business model intentionally assigns each application user exactly one primary business role.

This reduces ambiguity and keeps permission reasoning straightforward.

---

# 7. Authorization Policies

Roles do not inherently form a hierarchy in ASP.NET Core.

Therefore define explicit policies:

## EmployeeOrAbove

Allows:

- Employee
- Manager
- Admin

## ManagerOrAbove

Allows:

- Manager
- Admin

## AdminOnly

Allows:

- Admin

Endpoints should reference these policy names rather than repeatedly embedding long role strings.

This centralizes permission semantics.

---

# 8. Authentication Flow

## Login

1. Client obtains an antiforgery token.
2. Client submits email + password.
3. Identity locates ApplicationUser.
4. Application checks `IsActive`.
5. Identity verifies the password hash.
6. Lockout rules are evaluated.
7. On success, SignInManager creates the Identity application cookie.
8. Browser stores the cookie.
9. Future authenticated requests include the cookie automatically.

The API never returns or logs the password.

## Authenticated request

1. Request arrives with authentication cookie.
2. Authentication middleware validates cookie.
3. ASP.NET Core creates a ClaimsPrincipal.
4. Authorization policy checks identity/role.
5. Controller executes only if authorized.
6. Business services obtain the acting user through `ICurrentUserContext`.

## Logout

SignInManager removes/invalidates the browser authentication cookie.

---

# 9. Cookie Configuration

Recommended MVP authentication-cookie configuration:

- Name: `.BPInventory.Auth`
- HttpOnly: `true`
- Secure: always in production
- SameSite: `Strict`
- Expiration: approximately 8 hours
- No persistent "remember me" option in the MVP
- Fixed shift-like session rather than indefinitely sliding authentication

The exact expiration can be adjusted later, but it should be explicit rather than infinite.

---

# 10. Password Handling

## Password hashing

Passwords are one-way hashed by ASP.NET Core Identity's PasswordHasher.

The application does not encrypt passwords and does not need to know the plaintext after account creation/password entry.

Hashing and encryption are different:

- encryption is reversible with a key
- password hashing is intentionally one-way

Identity also manages salts/format information required to verify passwords securely.

## Password policy

MVP policy:

- minimum length: 10
- require lowercase
- require uppercase
- require digit
- require non-alphanumeric character

Passwords remain subject to Identity's validator.

Do not log passwords or include them in exception messages.

---

# 11. Account Lockout

Configure a basic lockout policy:

- lockout enabled for normal users
- 5 failed login attempts
- 10-minute lockout

Purpose:

Reduce repeated online password guessing.

Login responses should remain generic and should not reveal whether:

- the email does not exist
- the password is wrong
- the account is inactive

Operational/security logs may record relevant internal details without exposing them to the caller.

---

# 12. Account Activation / Deactivation

ApplicationUser includes:

`IsActive`

When an Admin deactivates a user:

1. set `IsActive = false`
2. update the user's Identity security stamp
3. persist the change
4. write an AuditLog entry

Future logins are rejected.

Existing authentication cookies become invalid when Identity next revalidates the security stamp.

Configure a relatively short security-stamp validation interval for the MVP, such as 5 minutes, so role/deactivation changes propagate without requiring a database lookup on every request.

---

# 13. Current User Abstraction

Create a scoped abstraction:

`ICurrentUserContext`

Purpose:

Allow application services to know the authenticated actor without depending directly on Controller or HttpContext APIs.

Conceptual properties:

- `UserId`
- `Email`
- `DisplayName`
- role information
- `IsAuthenticated`

Example:

```text
RestockService
    |
    +-- ApplicationDbContext
    |
    +-- ICurrentUserContext
```

RestockService obtains the current user from the trusted server-side context.

It never accepts:

`RecordedByUserId`

from a client request.

Benefits:

- avoids impersonation through request bodies
- keeps service code testable
- avoids leaking HTTP details throughout the business layer

---

# 14. CSRF Protection

Cookie authentication introduces Cross-Site Request Forgery risk because browsers automatically attach authentication cookies.

Therefore state-changing requests must use antiforgery protection.

## Design

Configure ASP.NET Core antiforgery support with a request-token header such as:

`X-CSRF-TOKEN`

Expose an endpoint such as:

`GET /api/auth/antiforgery-token`

The endpoint:

- creates/stores the antiforgery cookie
- returns the corresponding request token

The client includes that token in the configured header for unsafe methods.

Unsafe methods include:

- POST
- PUT
- PATCH
- DELETE

GET endpoints must never intentionally change business state.

## Defense in depth

Authentication cookie:

- HttpOnly
- Secure
- SameSite=Strict

Antiforgery token:

- proves that a state-changing browser request came from a client that obtained the token from the application

SameSite is useful protection, but antiforgery validation is the explicit CSRF control.

---

# 15. CORS

CORS controls which browser origins may call the API.

## MVP decision

Do not enable broad cross-origin access.

The backend/API documentation and any future simple frontend should preferably use the same origin.

Do not configure:

`AllowAnyOrigin`

just to make development errors disappear.

## If a separate frontend is added later

Allow only explicit configured origins.

With cookie authentication:

- client requests must include credentials
- server must explicitly allow credentials
- wildcard origins must not be combined with credentialed cookies
- SameSite/cookie behavior may need reevaluation
- antiforgery remains required

The project should revisit this configuration rather than blindly enabling cross-origin cookies.

---

# 16. HTTPS

Authentication credentials and cookies must travel over HTTPS.

Development:

- use the trusted ASP.NET Core/Visual Studio development HTTPS certificate

Azure:

- enforce HTTPS at Azure App Service / public edge
- mark authentication cookies Secure
- do not expose production login over plain HTTP

HTTPS protects data in transit.

HTTPS does NOT replace:

- authorization
- password hashing
- antiforgery protection
- validation

---

# 17. Secrets

Never commit:

- production connection strings with credentials
- seeded demo passwords
- API keys
- future third-party secrets
- private signing keys if token infrastructure is later added

Development secrets:

- .NET User Secrets when necessary

Azure deployment:

- Azure App Service application settings / connection-string configuration
- environment-backed ASP.NET Core configuration

A future Key Vault integration may be considered if it adds real value, but it is not required for the MVP.

---

# 18. Security Logging vs Audit Logging

## Technical/security logs

Examples:

- failed login
- lockout event
- unexpected authentication failure
- authorization failure diagnostics

Purpose:

Troubleshooting/security operations.

## AuditLog

Examples:

- Admin created User
- Admin changed User role
- Admin deactivated User
- Manager updated Product
- Employee recorded Restock

Purpose:

Business accountability.

Sensitive credential values must never be written to either.

---

# 19. Public Registration

There is NO public self-registration endpoint.

This is an internal employee system.

Users are provisioned by an Admin.

Reason:

A customer or arbitrary internet user should not be able to create an employee account.

---

# 20. Password Recovery / Email Confirmation

Deferred from the MVP.

Reasons:

- requires email-delivery infrastructure
- adds token lifecycle and account-recovery workflows
- not needed for the primary backend learning goals

A production workforce application would need a secure account-recovery process.

This omission must be documented rather than hidden.

---

# 21. Two-Factor Authentication

2FA is supported conceptually by ASP.NET Core Identity but is not required for the MVP.

It is a reasonable future security enhancement, especially for Admin accounts.

Do not add it before the core authentication and authorization flow works.

---

# 22. JWT / OAuth / OIDC Interview Knowledge

## JWT

A JSON Web Token is a token format containing claims and an integrity-protected signature.

An API can validate:

- signature
- issuer
- audience
- expiration
- relevant claims

A JWT is a format, not a complete authentication architecture.

## Bearer token

A bearer token is usable by whoever possesses it.

Therefore token theft matters.

## OAuth 2.0

A standardized authorization framework used to obtain access tokens.

## OpenID Connect

An identity layer built on OAuth concepts for user authentication.

## Future enterprise evolution

For a larger enterprise deployment:

```text
User
  |
  v
Microsoft Entra ID / OIDC
  |
  v
Access Token
  |
  v
BP Inventory API
  |
  v
JWT bearer validation + authorization policies
```

In that architecture, this API validates externally issued tokens instead of acting as its own token server.

---

# 23. Security Boundary Summary

The API trusts:

- authenticated ASP.NET Core Identity principal
- Identity-managed role membership
- server configuration
- validated database state

The API does NOT trust:

- client-supplied user IDs as actor identity
- client-supplied roles
- client-supplied timestamps for server audit events
- hidden UI buttons as authorization
- frontend validation alone
- raw request data before validation
