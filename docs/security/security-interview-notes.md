# Authentication and Security Interview Study Notes

## Authentication vs Authorization

Authentication:

> Who are you?

Authorization:

> What are you allowed to do?

Example:

- valid Identity cookie proves Employee Alice is signed in
- ManagerOrAbove policy determines Alice cannot create a Product if her role is Employee

---

## Hashing vs Encryption

Encryption:

- designed to be reversible with a key
- used when original plaintext must later be recovered

Password hashing:

- designed to be one-way
- passwords are verified by hashing/checking the candidate against stored hash metadata
- application should never need to recover the original password

ASP.NET Core Identity handles password hashing.

---

## Cookie Authentication vs JWT Bearer Authentication

### Cookie

Browser stores cookie and sends it automatically to matching requests.

Good for:

- browser-based same-site applications
- server-managed login sessions

Security considerations:

- HttpOnly
- Secure
- SameSite
- CSRF protection

### JWT bearer token

Client sends:

`Authorization: Bearer <token>`

Good for:

- APIs receiving standards-based tokens from an identity provider
- mobile/native clients
- service/API architectures
- delegated authorization scenarios

Security considerations:

- token acquisition must be secure
- signature/issuer/audience/expiration validation
- secure token storage
- expiry/refresh/revocation design

A JWT is not automatically "more modern" or "more secure" than cookies.

---

## Why We Chose Cookies

The project is a single internal business application with no independent mobile client or microservice ecosystem.

A secure Identity cookie:

- is simpler
- fits the expected browser client
- does not expose credentials/tokens to JavaScript
- avoids pretending the API is a production OAuth token server

If requirements later change, the authentication architecture can change without redesigning the Product/Inventory domain.

---

## Claims

A ClaimsPrincipal represents authenticated identity information.

Claims can describe facts such as:

- user identifier
- email
- role

Authorization evaluates the ClaimsPrincipal.

Do not place sensitive secrets into claims.

---

## Roles

Roles are coarse-grained groups:

- Employee
- Manager
- Admin

ASP.NET Core can restrict access by role.

Our project wraps hierarchy semantics in policies.

---

## Policies

A policy is a named authorization rule.

Examples:

`ManagerOrAbove`

requires role:

- Manager OR Admin

Policies are more expressive and maintainable than scattering role strings across controllers.

---

## 401 vs 403

401 Unauthorized:

> Authentication is missing or invalid.

403 Forbidden:

> Authentication succeeded, but permission is insufficient.

---

## CSRF

Cross-Site Request Forgery abuses the browser's willingness to automatically attach authentication context such as cookies.

Example:

1. User is logged into BP Inventory.
2. User visits a malicious website.
3. Malicious page attempts to submit a state-changing request to BP Inventory.
4. Browser may attach BP Inventory cookies automatically.

Antiforgery tokens require an additional value the malicious site cannot simply cause the browser to attach automatically.

---

## XSS vs CSRF

XSS:

Attacker gets script to execute in your site's origin.

CSRF:

Attacker causes a victim browser to submit an unintended request to your site.

HttpOnly cookies help prevent JavaScript from reading the authentication cookie, but XSS can still be dangerous because same-origin malicious code can perform actions as the user.

---

## CORS

CORS is a browser policy controlling which origins can read/interact with cross-origin responses.

CORS is NOT authentication.

CORS is NOT authorization.

CORS is NOT a general firewall.

Server authorization must still protect every sensitive endpoint.

---

## HTTPS

HTTPS protects confidentiality and integrity while data travels between client and server.

It does not tell the application whether an authenticated Employee is allowed to create Products.

HTTPS and authorization solve different problems.

---

## Security Stamp

Identity security stamps provide a mechanism for invalidating existing sign-in sessions after important account-security changes.

We update the stamp after actions such as:

- role changes
- user deactivation

Then Identity's periodic validation can reject stale sessions.

---

## Interview Summary

> I used ASP.NET Core Identity rather than implementing password storage or token security myself. The system uses secure cookie authentication because it is an internal browser-oriented application, with HttpOnly/Secure/SameSite protections and antiforgery validation for state-changing requests. Authorization is role-based but expressed through EmployeeOrAbove, ManagerOrAbove, and AdminOnly policies because ASP.NET roles don't inherently form a hierarchy. Business services derive the acting user from a scoped current-user abstraction instead of accepting user IDs from clients. User accounts are soft-deactivated to preserve historical attribution, security stamps are updated after role or activation changes, and credentials/secrets remain outside source control. For a future enterprise deployment with external clients or SSO, I would move authentication to a standards-based OIDC/OAuth identity provider such as Microsoft Entra ID and validate bearer access tokens rather than hand-rolling JWT issuance.
